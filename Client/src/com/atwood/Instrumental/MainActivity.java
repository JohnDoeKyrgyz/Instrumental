package com.atwood.Instrumental;

import android.app.Activity;
import android.hardware.Sensor;
import android.hardware.SensorManager;
import android.os.Bundle;
import android.view.MotionEvent;
import android.view.WindowManager;
import android.widget.TextView;
import com.atwood.Instrumental.communication.BasicSensorMessage;
import com.atwood.Instrumental.communication.Communication;
import com.atwood.Instrumental.communication.SensorMessage;

import java.util.LinkedList;
import java.util.List;

public class MainActivity extends Activity {
    private SensorManager sensorManager;
    private List<SensorEventListenerBase> listeners = new LinkedList<SensorEventListenerBase>();
    private Communication communication;
    private static final int TYPE_TOUCH_SENSOR = 69;

    /**
     * Called when the activity is first created.
     */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.main);

        //keep the screen on
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);

        //create location listener components
        sensorManager = (SensorManager) getSystemService(SENSOR_SERVICE);

        createScreenOutputListener(Sensor.TYPE_LINEAR_ACCELERATION,
                (TextView) findViewById(R.id.acceleratorX),
                (TextView) findViewById(R.id.acceleratorY),
                (TextView) findViewById(R.id.acceleratorZ));

        createScreenOutputListener(Sensor.TYPE_GAME_ROTATION_VECTOR,
                (TextView) findViewById(R.id.rotationX),
                (TextView) findViewById(R.id.rotationY),
                (TextView) findViewById(R.id.rotationZ));

        createScreenOutputListener(Sensor.TYPE_MAGNETIC_FIELD,
                (TextView) findViewById(R.id.magnetX),
                (TextView) findViewById(R.id.magnetY),
                (TextView) findViewById(R.id.magnetZ));

        createScreenOutputListener(Sensor.TYPE_GYROSCOPE,
                (TextView) findViewById(R.id.gyroX),
                (TextView) findViewById(R.id.gyroY),
                (TextView) findViewById(R.id.gyroZ));

        //create communication handler
        communication = new Communication(this, 5000);

        createNetworkOutputListener(Sensor.TYPE_LINEAR_ACCELERATION);
        createNetworkOutputListener(Sensor.TYPE_GAME_ROTATION_VECTOR);
        createNetworkOutputListener(Sensor.TYPE_MAGNETIC_FIELD);
        createNetworkOutputListener(Sensor.TYPE_GYROSCOPE);

        startListeners();
    }

    @Override
    public boolean dispatchTouchEvent(MotionEvent event) {
        boolean handled = super.dispatchTouchEvent(event);
        float x = event.getX();
        float y = event.getY();
        float[] values = {x, y};
        SensorMessage touchMessage = new BasicSensorMessage(TYPE_TOUCH_SENSOR, values);

        TextView touchX = (TextView) findViewById(R.id.touchX);
        TextView touchY = (TextView) findViewById(R.id.touchY);

        touchX.setText(String.valueOf(x));
        touchY.setText(String.valueOf(y));

        communication.getMessageQueue().add(touchMessage);
        return handled;
    }

    private void startListeners() {
        for (SensorEventListenerBase listener : listeners) listener.start();
        communication.connect();
    }

    private void createScreenOutputListener(int sensorType, TextView... textViews) {
        Sensor sensor = sensorManager.getDefaultSensor(sensorType);
        ScreenOutputSensorEventListener listener = new ScreenOutputSensorEventListener(sensorManager, sensor, textViews);
        listeners.add(listener);
    }

    private void createNetworkOutputListener(int sensorType) {
        Sensor sensor = sensorManager.getDefaultSensor(sensorType);
        QueueOutputSensorEventListener listener = new QueueOutputSensorEventListener(sensorManager, sensor, communication.getMessageQueue());
        listeners.add(listener);
    }

    @Override
    protected void onResume() {
        super.onResume();
        startListeners();
    }

    @Override
    protected void onPause() {
        super.onPause();
        stopListeners();
    }

    private void stopListeners() {
        for (SensorEventListenerBase listener : listeners) listener.stop();
        communication.disconnect();
    }
}
