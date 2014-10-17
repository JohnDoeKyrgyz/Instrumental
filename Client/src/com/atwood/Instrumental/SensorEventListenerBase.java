package com.atwood.Instrumental;

import android.hardware.Sensor;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;

/**
 * Created by johna on 9/23/2014.
 */
public abstract class SensorEventListenerBase implements SensorEventListener {
    protected SensorManager sensorManager;
    protected Sensor sensor;

    public SensorEventListenerBase(SensorManager sensorManager, Sensor sensor) {
        this.sensorManager = sensorManager;
        this.sensor = sensor;
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int i) {

    }

    public void start() {
        sensorManager.registerListener(this, this.sensor, SensorManager.SENSOR_DELAY_FASTEST);
    }

    public void stop() {
        sensorManager.unregisterListener(this);
    }
}
