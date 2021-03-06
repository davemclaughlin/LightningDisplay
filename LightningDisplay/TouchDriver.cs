using System;
using System.Threading;
using System.IO;
using System.Text;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.IO;
using Microsoft.SPOT.Touch;

public class DisplayNhd5
{
    //Some internal stuff
    private readonly byte[][] _registerValues;
    private readonly I2CDevice.I2CTransaction[] _readActions;
    private readonly int[] _previousState = new int[5];
    private readonly Int32[] _previousX = new Int32[5];
    private readonly Int32[] _previousY = new Int32[5];
    private readonly int[] _currentX = new int[5];
    private readonly int[] _currentY = new int[5];

    private readonly I2CDevice _sharedBus;

    //I2C configuration for FT5x06
    private readonly I2CDevice.Configuration _busConfiguration = new I2CDevice.Configuration(0x38, 400);


    /// <summary>
    /// Occurs when any of five fingers touches the screen. 
    /// </summary>
    /// <remarks></remarks>
    public event TouchEventHandler TouchDown = delegate { };

    /// <summary>
    /// Occurs when any of the touching fingers is raised of the screen.. 
    /// </summary>
    /// <remarks></remarks>
    public event TouchEventHandler TouchUp = delegate { };

    /// <summary>
    /// Occurs when a zoom-in gesture is detected. 
    /// </summary>
    /// <remarks>If one swipes fingers slow enough, multiple <see cref="ZoomIn"/> events are raised.</remarks>
    public event ZoomEventHandler ZoomIn = delegate { };

    /// <summary>
    /// Occurs when a zoom-out gesture is detected. 
    /// </summary>
    /// <remarks>If one swipes fingers slow enough, multiple <see cref="ZoomOut"/> events are raised.</remarks>
    public event ZoomEventHandler ZoomOut = delegate { };


    /// <summary>
    /// Initializes a new instance of the <see cref="T:Rhea.DisplayNhd5">DisplayNhd5</see> class. 
    /// </summary>
    /// <param name="sharedBus">An <see cref="I2CDevice"/> (in other words — I2C bus) used to poll for touch data.</param>
    /// <remarks>I2C bus configuration is kept internally and is set before each transaction.</remarks>
    public DisplayNhd5(I2CDevice sharedBus)
    {
        _sharedBus = sharedBus;

        //Initializing all finger states as "Touch up"
        _previousState[0] = 1;
        _previousState[1] = 1;
        _previousState[2] = 1;
        _previousState[3] = 1;
        _previousState[4] = 1;

        //To make things easier to read, array's index will correspond to actual register number. Beware that some registers do not exist, though.
        _registerValues = new byte[0x1E + 1][];
        var registerAddresses = new byte[0x1E + 1][];
        for (byte i = 0; i < 0x1E + 1; i++)
        {
            _registerValues[i] = new byte[1];
            registerAddresses[i] = new[] { i };
        }

        //Creating all the read transaction, so all important registers could be read in one batch, thus significantly speeding things up
        _readActions = new I2CDevice.I2CTransaction[44];
        for (var i = 1; i <= 6; i++)
        {
            _readActions[2 * (i - 1)] = I2CDevice.CreateWriteTransaction(registerAddresses[i]);
            _readActions[2 * (i - 1) + 1] = I2CDevice.CreateReadTransaction(_registerValues[i]);
        }

        for (var i = 0; i < 4; i++)
        {
            _readActions[2 * i + 12] = I2CDevice.CreateWriteTransaction(registerAddresses[i + 0x09]);
            _readActions[2 * i + 12 + 1] = I2CDevice.CreateReadTransaction(_registerValues[i + 0x09]);
        }

        for (var i = 0; i < 4; i++)
        {
            _readActions[2 * i + 12 + 8] = I2CDevice.CreateWriteTransaction(registerAddresses[i + 0x0F]);
            _readActions[2 * i + 12 + 8 + 1] = I2CDevice.CreateReadTransaction(_registerValues[i + 0x0F]);
        }
        for (var i = 0; i < 4; i++)
        {
            _readActions[2 * i + 12 + 16] = I2CDevice.CreateWriteTransaction(registerAddresses[i + 0x15]);
            _readActions[2 * i + 12 + 16 + 1] = I2CDevice.CreateReadTransaction(_registerValues[i + 0x15]);
        }
        for (var i = 0; i < 4; i++)
        {
            _readActions[2 * i + 12 + 24] = I2CDevice.CreateWriteTransaction(registerAddresses[i + 0x1B]);
            _readActions[2 * i + 12 + 24 + 1] = I2CDevice.CreateReadTransaction(_registerValues[i + 0x1B]);
        }
    }

    /// <summary>
    /// Reads touch data and raises events (if necessary).
    /// </summary>
    /// <remarks>I didn't include the interrupt handling here on purpose. This way one may choose to poll the screen continously, or use interrupts generated by FT5x06.</remarks>
    public void ReadAndProcessTouchData()
    {

        //getting data
        lock (_sharedBus)
        {
            _sharedBus.Config = _busConfiguration;
            if (_sharedBus.Execute(_readActions, 500) == 0)
            {
                Debug.Print("Failed to perform I2C transaction");
                return;
            }
        }


        int numberOfFingers = _registerValues[0x02][0];

        //            Debug.Print("Fingers = " + numberOfFingers);

        //Detecting gestures
        int gesture = _registerValues[0x01][0];
        if (gesture == 0x48) ZoomIn(this, new EventArgs());
        if (gesture == 0x49) ZoomOut(this, new EventArgs());

        //Parsing position data. Not all of that is always used, but should not hurt much.
        for (var i = 0; i < 5; i++)
        {
            _currentX[i] = ((_registerValues[0x03 + i * 6][0] & 0x0F) << 8) + _registerValues[0x04 + i * 6][0];
            _currentY[i] = ((_registerValues[0x05 + i * 6][0] & 0x0F) << 8) + _registerValues[0x06 + i * 6][0];
        }

        //FT5x06 logic is quite twisted. For example, we have two fingers touching the display, and we are tracking three fingers. In this case, Touch IDs (from registers 0x05, 0x0b, 0x11) will be 0, 1 and 0; their event flags(registers 0x03, 0x09 and 0x0f), accordingly, 2, 2 and 3:
        //	0:1:0 — 2:2:3 (no of touches=2, as reported by register 0x02)
        //Now, if a third finger touches the display, states will change to:
        //	0:1:2 — 2:2:0 (3)
        //Zero indicates it is a "Touch down" event, and will be sent only ONCE. Immediately after that, it'll go to
        //	0:1:2 — 2:2:2 (3)
        //So far so good: Touch IDs nicely correspond to Touch Events. But, if I now remove the second finger (with the ID = "1"), things will twist:
        //	0:2:0 — 2:2:1 (2!)
        //The third ID is "0", which is supposed to mean nothing, as number of touches is 2. But, for Touch Events, all three number are important: "1" signals "Touch up" event of the ID "1". However, in IDs, "1" is no longer present: it was removed and ID "2" shifted to its place. The same happens with touch coordinates: for this single transaction, touch data will be available in registers 0x0f-0x12, and what was in registers 0x0f-0x12 before this transaction, are moved to 0x09-0x0c, and stay there until another finger is removed from the display. This state is also sent only once. The next one will be:
        //	0:2:0 — 2:2:3 (2)

        //All this makes it very hard to fully utilize hardware touch up events. So I'll take half-software way to handle them.

        //Reading Touch IDs. This will be used later to see if the finger is on the screen, or not.
        var ids = new Int32[5];
        for (int i = 0; i < 5; i++)
        {
            ids[i] = _registerValues[0x05 + i * 6][0] >> 4;
        }


        for (int i = 0; i < 5; i++)
        {
            var thisFingerIsActive = false;
            for (int j = 0; j < numberOfFingers; j++)
            {
                if (i == ids[j])
                {
                    thisFingerIsActive = true;
                }
            }

            if (thisFingerIsActive)
            {
                //If the finger is active, it means either the finger has just touched the display, or it is already touching it. I only catch the first case (for now).
                if ((_previousState[i] != 0))
                {
                    _previousX[i] = _currentX[i];
                    _previousY[i] = _currentY[i];
                    _previousState[i] = 0;
                    TouchDown(this, new TouchEventArgs(_currentX[i], _currentY[i], i));
                }
            }
            else
            {
                //Finger is not active. Checking if it's time to raise a touch up event.
                if ((_previousState[i] != 1))
                {
                    _previousState[i] = 1;
                    TouchUp(this, new TouchEventArgs(_previousX[i], _previousY[i], i));
                }
            }
        }
    }

    /// <summary>
    /// Reads all the settings.
    /// </summary>
    /// <remarks>Reads all the settings from the FT5306</remarks>
    public void ReadSettings(byte StartAddr)
    {
        I2CDevice.I2CTransaction[] _readActions2 = new I2CDevice.I2CTransaction[12];
        I2CDevice.I2CReadTransaction readAction2;
        I2CDevice.I2CWriteTransaction writeAction2;
        byte[] _registerValues2 = new byte[10];
        byte dummy;

        writeAction2 = I2CDevice.CreateWriteTransaction(new byte[1] { StartAddr });
        readAction2 = I2CDevice.CreateReadTransaction(_registerValues2);

        _readActions2 = new I2CDevice.I2CTransaction[] { writeAction2, readAction2 };

        lock (_sharedBus)
        {
            _sharedBus.Config = _busConfiguration;
            if (_sharedBus.Execute(_readActions2, 500) == 0)
            {
                Debug.Print("Failed to perform I2C transaction");
            }
        }
        dummy = _registerValues2[0];
    }

    /// <summary>
    /// Reads all the settings.
    /// </summary>
    /// <remarks>Reads all the settings from the FT5306</remarks>
    public void ReadInformation()
    {
        I2CDevice.I2CTransaction[] _readActions2 = new I2CDevice.I2CTransaction[12];
        I2CDevice.I2CReadTransaction readAction2;
        I2CDevice.I2CWriteTransaction writeAction2;
        byte[] _registerValues2 = new byte[0x4C];
        byte dummy;

        //
        // Switch the test mode
        //
        writeAction2 = I2CDevice.CreateWriteTransaction(new byte[2] { 0x00, 0x04 });

        _readActions2 = new I2CDevice.I2CTransaction[] { writeAction2 };

        lock (_sharedBus)
        {
            _sharedBus.Config = _busConfiguration;
            if (_sharedBus.Execute(_readActions2, 500) == 0)
            {
                Debug.Print("Failed to perform I2C transaction");
            }
        }
        Thread.Sleep(100);
        writeAction2 = I2CDevice.CreateWriteTransaction(new byte[1] { 0x00 });
        readAction2 = I2CDevice.CreateReadTransaction(_registerValues2);

        _readActions2 = new I2CDevice.I2CTransaction[] { writeAction2, readAction2 };

        lock (_sharedBus)
        {
            _sharedBus.Config = _busConfiguration;
            if (_sharedBus.Execute(_readActions2, 500) == 0)
            {
                Debug.Print("Failed to perform I2C transaction");
            }
        }
        //
        // Switch the normal mode
        //
        writeAction2 = I2CDevice.CreateWriteTransaction(new byte[2] { 0x00, 0x00 });

        _readActions2 = new I2CDevice.I2CTransaction[] { writeAction2 };

        lock (_sharedBus)
        {
            _sharedBus.Config = _busConfiguration;
            if (_sharedBus.Execute(_readActions2, 500) == 0)
            {
                Debug.Print("Failed to perform I2C transaction");
            }
        }
        Thread.Sleep(100);
        dummy = _registerValues2[0];
    }

    /// <summary>
    /// Runs a calibration on the touch display
    /// </summary>
    /// <remarks>Calibrates the FT5306</remarks>
    public void Calibrtate()
    {
        I2CDevice.I2CTransaction[] _readActions2 = new I2CDevice.I2CTransaction[12];
        I2CDevice.I2CReadTransaction readAction2;
        I2CDevice.I2CWriteTransaction writeAction2;
        byte[] _registerValues2 = new byte[10];
        byte dummy;
        //
        // Enable calibration mode
        //
        Debug.Print("Calibration started");

        writeAction2 = I2CDevice.CreateWriteTransaction(new byte[2] { 0x00, 0x00 });

        _readActions2 = new I2CDevice.I2CTransaction[] { writeAction2 };

        lock (_sharedBus)
        {
            _sharedBus.Config = _busConfiguration;
            if (_sharedBus.Execute(_readActions2, 500) == 0)
            {
                Debug.Print("Failed to perform I2C transaction");
            }
        }
        Thread.Sleep(100);
        //
        // Start the calibration
        //
        writeAction2 = I2CDevice.CreateWriteTransaction(new byte[2] { 0xA0, 0x00 });

        _readActions2 = new I2CDevice.I2CTransaction[] { writeAction2 };

        lock (_sharedBus)
        {
            _sharedBus.Config = _busConfiguration;
            if (_sharedBus.Execute(_readActions2, 500) == 0)
            {
                Debug.Print("Failed to perform I2C transaction");
            }
        }
        Thread.Sleep(300);
        int Index = 0;
        byte[] _registerValues1 = new byte[1];
        while (Index < 100)
        {
            writeAction2 = I2CDevice.CreateWriteTransaction(new byte[1] { 0xA7 });
            readAction2 = I2CDevice.CreateReadTransaction(_registerValues1);

            _readActions2 = new I2CDevice.I2CTransaction[] { writeAction2, readAction2 };

            lock (_sharedBus)
            {
                _sharedBus.Config = _busConfiguration;
                if (_sharedBus.Execute(_readActions2, 500) == 0)
                {
                    Debug.Print("Failed to perform I2C transaction");
                }
            }
            if(_registerValues1[0] == 4)    // Calibration running?
            {
                break;
            }
            Debug.Print("Waiting calibration " + Index.ToString());
            Thread.Sleep(300);
            Index++;
        }
        Debug.Print("Calibration OK");
        //
        // Disable auto calibration
        //
        writeAction2 = I2CDevice.CreateWriteTransaction(new byte[2] { 0xA0, 0xFF });

        _readActions2 = new I2CDevice.I2CTransaction[] { writeAction2 };

        lock (_sharedBus)
        {
            _sharedBus.Config = _busConfiguration;
            if (_sharedBus.Execute(_readActions2, 500) == 0)
            {
                Debug.Print("Failed to perform I2C transaction");
            }
        }
        Thread.Sleep(100);
        //
        // Set to normal mode
        //
        writeAction2 = I2CDevice.CreateWriteTransaction(new byte[2] { 0x00, 0x00 });

        _readActions2 = new I2CDevice.I2CTransaction[] { writeAction2 };

        lock (_sharedBus)
        {
            _sharedBus.Config = _busConfiguration;
            if (_sharedBus.Execute(_readActions2, 500) == 0)
            {
                Debug.Print("Failed to perform I2C transaction");
            }
        }
        Thread.Sleep(300);
        dummy = _registerValues2[0];
        Debug.Print("Calibration stored");
    }
}

public delegate void TouchEventHandler(DisplayNhd5 sender, TouchEventArgs e);
public delegate void ZoomEventHandler(DisplayNhd5 sender, EventArgs e);

public class TouchEventArgs : EventArgs
{
    public Int32 FingerNumber { get; private set; }
    public Int32 X { get; private set; }
    public Int32 Y { get; private set; }

    public TouchEventArgs(Int32 x, Int32 y, Int32 fingerNumber)
    {
        FingerNumber = fingerNumber;
        X = x;
        Y = y;
    }
}
