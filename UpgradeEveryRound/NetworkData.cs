using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace UpgradeEveryRound
{
    [Serializable()]
    public record NetworkData
    {
        public int upgradesPerRound;
        public bool limitedChoices;
        public int numChoices;

        public bool allowMapCount;
        public bool allowEnergy;
        public bool allowExtraJump;
        public bool allowRange;
        public bool allowStrength;
        public bool allowHealth;
        public bool allowSpeed;
        public bool allowTumbleLaunch;
        public int[] extraData;

        public NetworkData()
        {
            upgradesPerRound = Plugin.upgradesPerRound.Value;
            limitedChoices = Plugin.limitedChoices.Value;
            numChoices = Plugin.numChoices.Value;

            allowMapCount = Plugin.allowMapCount.Value;
            allowEnergy = Plugin.allowEnergy.Value;
            allowExtraJump = Plugin.allowExtraJump.Value;
            allowRange = Plugin.allowRange.Value;
            allowStrength = Plugin.allowStrength.Value;
            allowHealth = Plugin.allowHealth.Value;
            allowSpeed = Plugin.allowSpeed.Value;
            allowTumbleLaunch = Plugin.allowTumbleLaunch.Value;
            extraData = Plugin.ExtraConfigs.Select(config => config.Data).ToArray(); // serializing the packed bits instead of a bool array
        }

        public static byte[] Serealize(object data)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, data);
                return ms.ToArray();
            }
        }
        public static NetworkData Deserialize(byte[] arrBytes)
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = (NetworkData)binForm.Deserialize(memStream);
                return obj;
            }
        }
    }
}