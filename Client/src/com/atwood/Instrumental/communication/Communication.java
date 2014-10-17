package com.atwood.Instrumental.communication;

import android.content.Context;
import android.hardware.SensorEvent;
import android.net.DhcpInfo;
import android.net.wifi.WifiManager;
import android.util.Log;

import java.io.IOException;
import java.net.*;
import java.nio.ByteBuffer;
import java.util.concurrent.ConcurrentLinkedQueue;

public class Communication {

    private Context context;
    private int port;
    private static boolean running;
    private ConcurrentLinkedQueue<SensorMessage> messageQueue = new ConcurrentLinkedQueue<SensorMessage>();

    private InetAddress getBroadcastAddress() throws IOException {
        WifiManager wifi = (WifiManager) context.getSystemService(Context.WIFI_SERVICE);
        DhcpInfo dhcp = wifi.getDhcpInfo();
        if (dhcp == null) {
            throw new IOException("No dhcp information available");
        }

        int broadcast = (dhcp.ipAddress & dhcp.netmask) | ~dhcp.netmask;
        byte[] quads = new byte[4];
        for (int k = 0; k < 4; k++)
            quads[k] = (byte) ((broadcast >> k * 8) & 0xFF);

        InetAddress address = InetAddress.getByAddress(quads);

        Log.i(TAG, String.format("Connecting to %s", address));

        return address;
    }

    public Communication(Context context, int port) {
        this.context = context;
        this.port = port;
    }

    private final String TAG = "COMMUNICATION";

    public void connect() {
        if(!running){
            running = true;

            Thread runner = new Thread() {

                private MulticastSocket socket = null;

                @Override
                protected void finalize() throws Throwable {
                    super.finalize();

                    if(socket != null){
                        socket.disconnect();
                    }
                }

                @Override
                public void run() {

                    //open socket connection
                    InetAddress broadcastAddress = null;
                    try {
                        socket = new MulticastSocket(port);
                        socket.setBroadcast(true);
                        socket.setReuseAddress(true);
                        broadcastAddress = getBroadcastAddress();
                    } catch (IOException e) {
                        Log.e(TAG, "Could not open socket", e);
                    }

                    if(socket != null){

                        Log.i(TAG,"Sending messages");

                        //send messages
                        while (running) {
                            SensorMessage message = Communication.this.messageQueue.poll();
                            if (message != null) {
                                ByteBuffer buffer = encodeMessage(message);
                                try {
                                    DatagramPacket packet = new DatagramPacket(buffer.array(), buffer.capacity(), broadcastAddress, port);
                                    socket.send(packet);
                                } catch (IOException e) {
                                    running = false;
                                    Log.e(TAG, "Could not send message", e);
                                }
                            } else {
                                try {
                                    Thread.sleep(10);
                                } catch (InterruptedException e) {
                                    //do nothing
                                }
                            }
                        }

                        //close the socket
                        socket.close();
                        socket = null;
                        Log.i(TAG,"Finished sending messages");
                    }
                }
            };

            runner.setDaemon(true);
            runner.start();
        }
    }

    private ByteBuffer encodeMessage(SensorMessage message) {
        int sensor = message.getType();
        float[] values = message.getValues();

        int size = 12 + values.length * 4;
        ByteBuffer buffer = ByteBuffer.allocate(size);

        buffer.putLong(message.getTime());
        buffer.putInt(sensor);
        for (float value : values) buffer.putFloat(value);

        return buffer;
    }

    public void disconnect() {
        running = false;
    }

    public ConcurrentLinkedQueue<SensorMessage> getMessageQueue() {
        return messageQueue;
    }
}
