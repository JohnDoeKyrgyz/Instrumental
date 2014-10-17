package com.atwood.Instrumental.communication;

public interface SensorMessage {
    long getTime();

    float[] getValues();

    int getType();
}

