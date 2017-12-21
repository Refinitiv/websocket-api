/*
 * Copyright (C) 2015-2016 Neo Visionaries Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
package com.neovisionaries.ws.client;


import java.util.Timer;
import java.util.TimerTask;


abstract class PeriodicalFrameSender
{
    private final WebSocket mWebSocket;
    private final String mTimerName;
    private Timer mTimer;
    private boolean mScheduled;
    private long mInterval;
    private PayloadGenerator mGenerator;


    public PeriodicalFrameSender(
            WebSocket webSocket, String timerName, PayloadGenerator generator)
    {
        mWebSocket = webSocket;
        mTimerName = timerName;
        mGenerator = generator;
    }


    public void start()
    {
        setInterval(getInterval());
    }


    public void stop()
    {
        synchronized (this)
        {
            if (mTimer == null)
            {
                return;
            }

            mScheduled = false;
            mTimer.cancel();
        }
    }


    public long getInterval()
    {
        synchronized (this)
        {
            return mInterval;
        }
    }


    public void setInterval(long interval)
    {
        if (interval < 0)
        {
            interval = 0;
        }

        synchronized (this)
        {
            mInterval = interval;
        }

        if (interval == 0)
        {
            return;
        }

        if (mWebSocket.isOpen() == false)
        {
            return;
        }

        synchronized (this)
        {
            if (mTimer == null)
            {
                mTimer = new Timer(mTimerName);
            }

            if (mScheduled == false)
            {
                mScheduled = true;
                mTimer.schedule(new Task(), interval);
            }
        }
    }


    public PayloadGenerator getPayloadGenerator()
    {
        synchronized (this)
        {
            return mGenerator;
        }
    }


    public void setPayloadGenerator(PayloadGenerator generator)
    {
        synchronized (this)
        {
            mGenerator = generator;
        }
    }


    private final class Task extends TimerTask
    {
        @Override
        public void run()
        {
            doTask();
        }
    }


    private void doTask()
    {
        synchronized (this)
        {
            if (mInterval == 0 || mWebSocket.isOpen() == false)
            {
                mScheduled = false;

                // Not schedule a new task.
                return;
            }

            // Create a frame and send it to the server.
            mWebSocket.sendFrame(createFrame());

            // Schedule a new task.
            mTimer.schedule(new Task(), mInterval);
        }
    }


    private WebSocketFrame createFrame()
    {
        // Prepare payload of a frame.
        byte[] payload = generatePayload();

        // Let the subclass create a frame.
        return createFrame(payload);
    }


    private byte[] generatePayload()
    {
        if (mGenerator == null)
        {
            return null;
        }

        try
        {
            // Let the generator generate payload.
            return mGenerator.generate();
        }
        catch (Throwable t)
        {
            // Empty payload.
            return null;
        }
    }


    protected abstract WebSocketFrame createFrame(byte[] payload);
}
