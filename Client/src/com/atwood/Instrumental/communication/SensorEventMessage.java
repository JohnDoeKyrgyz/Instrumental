package com.atwood.Instrumental.communication;

import android.hardware.SensorEvent;

public class SensorEventMessage implements SensorMessage{
    private final long time;
    private SensorEvent event;

    public SensorEventMessage(SensorEvent event) {
        this.event = event;
        this.time = System.nanoTime();
    }

    @Override
    public long getTime() {
        return time;
    }

    @Override
    public float[] getValues() {
        return event.values;
    }

    @Override
    public int getType() {
        return event.sensor.getType();
    }
}
