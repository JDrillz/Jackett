﻿using Jackett.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.IndexerConfig
{
    public abstract class ConfigurationData
    {
        public enum ItemType
        {
            InputString,
            InputBool,
            DisplayImage,
            DisplayInfo,
            HiddenData
        }

        public HiddenItem CookieHeader { get; private set; } = new HiddenItem { Name = "CookieHeader" };

        public ConfigurationData()
        {

        }

        public ConfigurationData(JToken json)
        {
            LoadValuesFromJson(json);
        }

        public void LoadValuesFromJson(JToken json)
        {
            var arr = (JArray)json;
            foreach (var item in GetItems(forDisplay: false))
            {
                var arrItem = arr.FirstOrDefault(f => f.Value<string>("id") == item.ID);
                if (arrItem == null)
                    continue;

                switch (item.ItemType)
                {
                    case ItemType.InputString:
                        ((StringItem)item).Value = arrItem.Value<string>("value");
                        break;
                    case ItemType.HiddenData:
                        ((HiddenItem)item).Value = arrItem.Value<string>("value");
                        break;
                    case ItemType.InputBool:
                        ((BoolItem)item).Value = arrItem.Value<bool>("value");
                        break;
                }
            }
        }

        public JToken ToJson(bool forDisplay = true)
        {
            var items = GetItems(forDisplay);
            var jArray = new JArray();
            foreach (var item in items)
            {
                var jObject = new JObject();
                jObject["id"] = item.ID;
                jObject["type"] = item.ItemType.ToString().ToLower();
                jObject["name"] = item.Name;
                switch (item.ItemType)
                {
                    case ItemType.InputString:
                    case ItemType.HiddenData:
                    case ItemType.DisplayInfo:
                        jObject["value"] = ((StringItem)item).Value;
                        break;
                    case ItemType.InputBool:
                        jObject["value"] = ((BoolItem)item).Value;
                        break;
                    case ItemType.DisplayImage:
                        string dataUri = DataUrlUtils.BytesToDataUrl(((ImageItem)item).Value, "image/jpeg");
                        jObject["value"] = dataUri;
                        break;
                }
                jArray.Add(jObject);
            }
            return jArray;
        }

        Item[] GetItems(bool forDisplay)
        {
            var properties = GetType()
                .GetProperties()
                .Where(p => p.CanRead)
                .Where(p => p.PropertyType.IsSubclassOf(typeof(Item)))
                .Select(p => (Item)p.GetValue(this));

            if (!forDisplay)
            {
                properties = properties
                    .Where(p => p.ItemType == ItemType.HiddenData || p.ItemType == ItemType.InputBool || p.ItemType == ItemType.InputString)
                    .ToArray();
            }

            return properties.ToArray();
        }

        public class Item
        {
            public ItemType ItemType { get; set; }
            public string Name { get; set; }
            public string ID { get { return Name.Replace(" ", "").ToLower(); } }
        }

        public class HiddenItem : StringItem
        {
            public HiddenItem(string value = "")
            {
                Value = value;
                ItemType = ItemType.HiddenData;
            }
        }

        public class DisplayItem : StringItem
        {
            public DisplayItem(string value)
            {
                Value = value;
                ItemType = ItemType.DisplayInfo;
            }
        }

        public class StringItem : Item
        {
            public string Value { get; set; }
            public StringItem()
            {
                ItemType = ConfigurationData.ItemType.InputString;
            }
        }

        public class BoolItem : Item
        {
            public bool Value { get; set; }
            public BoolItem()
            {
                ItemType = ConfigurationData.ItemType.InputBool;
            }
        }

        public class ImageItem : Item
        {
            public byte[] Value { get; set; }
            public ImageItem()
            {
                ItemType = ConfigurationData.ItemType.DisplayImage;
            }
        }

        //public abstract Item[] GetItems();
    }
}