﻿using Multitool.Data.Media;
using Multitool.Data.Settings.Converters;

using System;
using System.Xml;

namespace MultitoolWinUI.Models
{
    public class PlaylistModelSettingConverter : ISettingConverter
    {
        public XmlNode Convert(object toConvert)
        {
            if (toConvert is PlaylistModel model)
            {
                XmlDocument doc = new();
                XmlNode node = doc.CreateElement("Playlist");
                var name = doc.CreateAttribute(nameof(model.Name));
                name.Value = model.Name;
                var desc = doc.CreateAttribute(nameof(model.Description));
                desc.Value = model.Description;
                node.Attributes.Append(name);
                node.Attributes.Append(desc);

                foreach (var song in model.Playlist)
                {
                    XmlNode songNode = doc.CreateElement(nameof(String));
                    var value = doc.CreateAttribute("Value");
                    value.Value = song.Path;
                    songNode.Attributes.Append(value);
                    node.AppendChild(songNode);
                }

                return node;
            }
            return null;
        }

        public object Restore(XmlNode toRestore)
        {
            var name = toRestore.Attributes[nameof(PlaylistModel.Name)]?.Value;
            if (!string.IsNullOrEmpty(name))
            {
                string description = toRestore.Attributes[nameof(PlaylistModel.Description)]?.Value ?? string.Empty;
                Playlist playlist = new()
                {
                    Description = description,
                    Name = name
                };
                if (toRestore.HasChildNodes)
                {
                    foreach (XmlNode node in toRestore.ChildNodes)
                    {
                        var value = node.Attributes["Value"];
                        if (value != null)
                        {
                            playlist.AddFile(value.ToString()).Wait();
                        }
                    }

                }
                PlaylistModel model = new(playlist);
                return model;
            }
            return null;
        }

        public object Restore(object defaultValue)
        {
            return null;
        }
    }
}
