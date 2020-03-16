//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|           Copyright (C) 2019 Refinitiv. All rights reserved.              --
//|-----------------------------------------------------------------------------


package com.refinitiv.android;

import android.os.Bundle;
import android.support.v7.app.AppCompatActivity;
import android.text.Editable;
import android.text.InputType;
import android.text.method.KeyListener;
import android.view.View;
import android.widget.Button;
import android.widget.CheckBox;
import android.widget.CompoundButton;
import android.widget.EditText;
import android.widget.RadioGroup;
import android.widget.TableLayout;
import android.widget.TableRow;
import android.widget.TextView;

import org.json.JSONException;

import java.util.ArrayList;

public class MainActivity extends AppCompatActivity {

    public static WebSocketTask ss = null;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        RadioGroup rg = (RadioGroup) findViewById(R.id.radioGroup);
        rg.check(R.id.mpRadioButton);

        Button clickButton = (Button) findViewById(R.id.button);
        if (clickButton != null) {
            clickButton.setOnClickListener(new View.OnClickListener() {

                @Override
                public void onClick(View v) {
                    // TODO Auto-generated method stub
                    String hostname = ((EditText)findViewById(R.id.hostname_field)).getText().toString();
                    String port = ((EditText)findViewById(R.id.port_field)).getText().toString();
                    String authHostname = ((EditText)findViewById(R.id.auth_hostname_field)).getText().toString();
                    String authPort = ((EditText)findViewById(R.id.auth_port_field)).getText().toString();
                    String username = ((EditText)findViewById(R.id.username_field)).getText().toString();
                    String password = ((EditText)findViewById(R.id.password_field)).getText().toString();
                    boolean authentication = ((CheckBox)findViewById(R.id.authentication_check_box)).isChecked();
                    String appId = "256";
                    if(authentication) {
                        appId = "555";
                    }
                    RadioGroup rg = (RadioGroup) findViewById(R.id.radioGroup);
                    WebSocketTask.Request requestType;
                    switch(rg.getCheckedRadioButtonId()) {
                        case R.id.mpRadioButton:
                            requestType = WebSocketTask.Request.MARKET_PRICE;
                            break;
                        case R.id.mpPostingRadioButton:
                            requestType = WebSocketTask.Request.MARKET_PRICE_POST;
                            break;
                        case R.id.mpBVRadioButton:
                            requestType = WebSocketTask.Request.MARKET_PRICE_BV;
                            break;
                        default:
                            requestType = WebSocketTask.Request.MARKET_PRICE;
                            break;
                    }

                    ss = new WebSocketTask((TextView)findViewById(R.id.maintextview), hostname, port, username, password, authHostname, authPort, appId, authentication, requestType);
                    ss.execute();
                }
            });
        }

        Button closeButton = (Button) findViewById(R.id.close_button);
        if (closeButton != null) {
            closeButton.setOnClickListener(new View.OnClickListener() {

                @Override
                public void onClick(View v) {
                    // TODO Auto-generated method stub
                    if (ss != null) {
                        try {
                            ss.closeConnection();
                        } catch (JSONException e) {
                            e.printStackTrace();
                        }

                        ss.cancel(true);
                        ss = null;
                    }
                }
            });
        }

        Button clearButton = (Button) findViewById(R.id.clear_button);
        if (clearButton != null) {
            clearButton.setOnClickListener(new View.OnClickListener() {

                @Override
                public void onClick(View v) {
                    // TODO Auto-generated method stub
                    ((TextView)findViewById(R.id.maintextview)).setText("");
                }
            });
        }

        CheckBox authCheckBox = (CheckBox)findViewById(R.id.authentication_check_box);
        authCheckBox.setOnCheckedChangeListener(new CompoundButton.OnCheckedChangeListener() {
            @Override
            public void onCheckedChanged(CompoundButton buttonView, boolean isChecked) {

                float alpha = (isChecked) ? (float)1.0 : (float)0.5;
                TextView authHostnameLabel = (TextView)findViewById(R.id.auth_hostname_label);
                TextView authPortLabel = (TextView)findViewById(R.id.auth_port_label);
                TextView usernameLabel = (TextView)findViewById(R.id.username_label);
                TextView passwordLabel = (TextView)findViewById(R.id.password_label);
                EditText authHostnameField = (EditText)findViewById(R.id.auth_hostname_field);
                EditText authPortField = (EditText)findViewById(R.id.auth_port_field);
                EditText usernameField = (EditText)findViewById(R.id.username_field);
                EditText passwordField = (EditText)findViewById(R.id.password_field);

                authHostnameField.setEnabled(isChecked);
                authHostnameField.setAlpha(alpha);
                authHostnameLabel.setAlpha(alpha);
                authPortField.setEnabled(isChecked);
                authPortField.setAlpha(alpha);
                authPortLabel.setAlpha(alpha);
                usernameField.setEnabled(isChecked);
                usernameField.setAlpha(alpha);
                usernameLabel.setAlpha(alpha);
                passwordField.setEnabled(isChecked);
                passwordField.setAlpha(alpha);
                passwordLabel.setAlpha(alpha);
            }
        });
    }
}
