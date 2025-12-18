using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace WitShells.XR.Utils
{
    public static class Utils
    {
        public static bool IsHandActuallyTracked()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.HandTracking,
                devices);

            foreach (var device in devices)
            {
                if (device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked)
                    return true;
            }
            return false;
        }


        public static bool IsControllerActuallyTracked()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Controller,
                devices);

            foreach (var device in devices)
            {
                if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
                    return true;
            }
            return false;
        }

    }
}