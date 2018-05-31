using System;
using System.Diagnostics;
using System.Text;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SimpleBleExample_by_Devicename
{
   /// <summary>
   /// An empty page that can be used on its own or navigated to within a Frame.
   /// </summary>
   public sealed partial class MainPage : Page
   {
      GattDeviceService service = null;
      GattCharacteristic charac = null;
      Guid MyService_GUID;
      Guid MYCharacteristic_GUID;
      string bleDevicName = "KEY";// !!!your device name!!!
      long deviceFoundMilis = 0, serviceFoundMilis = 0;
      long connectedMilis = 0, characteristicFoundMilis = 0;
      long WriteDescriptorMilis = 0;
      Stopwatch stopwatch;

      public MainPage()
      {
         this.InitializeComponent();
         stopwatch = new Stopwatch();
         // !!!Your service !!!
         MyService_GUID = new Guid("0000ffe0-0000-1000-8000-00805f9b34fb");
         //!!!Your characteristic!!!
         MYCharacteristic_GUID = new Guid("{0000ffe1-0000-1000-8000-00805f9b34fb}");
         StartWatching();
      }

      private void StartWatching()
      {
         // Create Bluetooth Listener
         var watcher = new BluetoothLEAdvertisementWatcher
         {  
            //Set scanning mode.
            //Active means get all the possible information in the advertisement data.
            //Use Passive if you already know the Ble-Address and only want to connect.
            //Scanning mode Passive is Action lot faster.
            ScanningMode = BluetoothLEScanningMode.Active
         };
         // Register callback for when we see an advertisements
         watcher.Received += OnAdvertisementReceivedAsync;
         stopwatch.Start();
         watcher.Start();
      }


      private async void OnAdvertisementReceivedAsync(BluetoothLEAdvertisementWatcher watcher,
                                                      BluetoothLEAdvertisementReceivedEventArgs eventArgs)
      {
         // Filter for specific Device by name
         if (eventArgs.Advertisement.LocalName == bleDevicName)
         {
            watcher.Stop();
            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(eventArgs.BluetoothAddress);
            //always check for null!!
            if (device != null)
            {
               deviceFoundMilis = stopwatch.ElapsedMilliseconds;
               Debug.WriteLine("Device found in " + deviceFoundMilis + " ms");

               var rssi = eventArgs.RawSignalStrengthInDBm;
               Debug.WriteLine("Signalstrengt = " + rssi + " DBm");

               var bleAddress = eventArgs.BluetoothAddress;
               Debug.WriteLine("Ble address = " + bleAddress);

               var advertisementType = eventArgs.AdvertisementType;              
               Debug.WriteLine("Advertisement type = " + advertisementType);
              
               var result = await device.GetGattServicesForUuidAsync(MyService_GUID);
               if (result.Status == GattCommunicationStatus.Success)
               {
                  connectedMilis = stopwatch.ElapsedMilliseconds;
                  Debug.WriteLine("Connected in " + (connectedMilis - deviceFoundMilis) + " ms");
                  var services = result.Services;
                  service = services[0];
                  if (service != null)
                  {
                     serviceFoundMilis = stopwatch.ElapsedMilliseconds;
                     Debug.WriteLine("Service found in " +
                        (serviceFoundMilis - connectedMilis) + " ms");
                     var charResult = await service.GetCharacteristicsForUuidAsync(MYCharacteristic_GUID);
                     if (charResult.Status == GattCommunicationStatus.Success)
                     {
                        charac = charResult.Characteristics[0];
                        if (charac != null)
                        {
                           characteristicFoundMilis = stopwatch.ElapsedMilliseconds;
                           Debug.WriteLine("Characteristic found in " +
                                          (characteristicFoundMilis - serviceFoundMilis) + " ms");

                           var descriptorValue = GattClientCharacteristicConfigurationDescriptorValue.None;
                           GattCharacteristicProperties properties = charac.CharacteristicProperties;
                           string descriptor = string.Empty;

                           if (properties.HasFlag(GattCharacteristicProperties.Read))
                           {
                              Debug.WriteLine("This characteristic supports reading .");
                           }
                           if (properties.HasFlag(GattCharacteristicProperties.Write))
                           {
                              Debug.WriteLine("This characteristic supports writing .");
                           }
                           if (properties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
                           {
                              Debug.WriteLine("This characteristic supports writing  whithout responce.");
                           }
                           if (properties.HasFlag(GattCharacteristicProperties.Notify))
                           {
                              descriptor = "notifications";
                              descriptorValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
                              Debug.WriteLine("This characteristic supports subscribing to notifications.");
                           }
                           if (properties.HasFlag(GattCharacteristicProperties.Indicate))
                           {
                              descriptor = "indications";
                              descriptorValue = GattClientCharacteristicConfigurationDescriptorValue.Indicate;
                              Debug.WriteLine("This characteristic supports subscribing to Indication");
                           }
                           try
                           {
                              var descriptorWriteResult = await charac.WriteClientCharacteristicConfigurationDescriptorAsync(descriptorValue);
                              if (descriptorWriteResult == GattCommunicationStatus.Success)
                              {
                                 
                                 WriteDescriptorMilis = stopwatch.ElapsedMilliseconds;
                                 Debug.WriteLine("Successfully registered for " + descriptor +" in " +
                                                (WriteDescriptorMilis - characteristicFoundMilis) + " ms");
                                 charac.ValueChanged += Charac_ValueChanged; ;
                              }
                              else
                              {
                                 Debug.WriteLine($"Error registering for " + descriptor + ": {result}");
                                 device.Dispose();
                                 device = null;
                                 watcher.Start();//Start watcher again for retry
                              }
                           }
                           catch (UnauthorizedAccessException ex)
                           {
                              Debug.WriteLine(ex.Message);
                           }
                        }
                     }
                     else Debug.WriteLine("No characteristics  found");
                  }
               }
               else Debug.WriteLine("No services found");
            }
            else Debug.WriteLine("No device found");
         }
      }
      private static void Charac_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
      {
         CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out byte[] data);
         //If data is raw bytes skip all the next lines and use data byte array. Or
         //CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out byte[] dataArray);
         string dataFromNotify;
         try
         {
            //Asuming Encoding is in ASCII, can be UTF8 or other!
            dataFromNotify = Encoding.ASCII.GetString(data);
            Debug.Write(dataFromNotify);
         }
         catch (ArgumentException)
         {
            Debug.Write("Unknown format");
         }
      }
   }
}


