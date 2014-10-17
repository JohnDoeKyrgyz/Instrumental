package com.atwood.Instrumental;

import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.widget.TextView;

public class ScreenOutputSensorEventListener extends SensorEventListenerBase {
    private TextView[] measurementTextViews;

    public ScreenOutputSensorEventListener(SensorManager sensorManager, Sensor sensor, TextView... measurementTextViews){
        super(sensorManager, sensor);
        this.measurementTextViews = measurementTextViews;
    }

    @Override
    public void onSensorChanged(SensorEvent sensorEvent) {

        for (int i = 0; i < measurementTextViews.length; i++) {
            TextView measurementTextView = measurementTextViews[i];
            float value = sensorEvent.values[i];
            measurementTextView.setText(Float.toString(value));
        }
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int i) {

    }

}
