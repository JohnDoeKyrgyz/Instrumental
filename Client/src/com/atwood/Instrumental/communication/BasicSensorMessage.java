package com.atwood.Instrumental.communication;

public class BasicSensorMessage implements SensorMessage {

    private int type;
    private float[] values;
    private long time;

    public BasicSensorMessage(int type, float[] values) {
        this.type = type;
        this.values = values;
        this.time = System.nanoTime();
    }

    @Override
    public long getTime() {
        return time;
    }

    @Override
    public float[] getValues() {
        return values;
    }

    @Override
    public int getType() {
        return type;
    }
}
