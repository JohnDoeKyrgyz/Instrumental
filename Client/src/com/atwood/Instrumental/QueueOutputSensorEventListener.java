package com.atwood.Instrumental;

import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorManager;
import com.atwood.Instrumental.communication.SensorEventMessage;
import com.atwood.Instrumental.communication.SensorMessage;

import java.util.concurrent.ConcurrentLinkedQueue;

public class QueueOutputSensorEventListener extends SensorEventListenerBase {

    private final ConcurrentLinkedQueue<SensorMessage> queue;

    public QueueOutputSensorEventListener(SensorManager sensorManager, Sensor sensor, ConcurrentLinkedQueue<SensorMessage> queue) {
        super(sensorManager, sensor);
        this.queue = queue;
    }

    @Override
    public void onSensorChanged(SensorEvent sensorEvent) {
        this.queue.add(new SensorEventMessage(sensorEvent));
    }
}
