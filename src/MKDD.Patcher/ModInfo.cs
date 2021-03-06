﻿using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace MKDD.Patcher
{
    [XmlRoot("mod")]
    public class ModInfo
    {
        public const string FILENAME = "mod.xml";

        [XmlElement( "title")] public string Title { get; set; }
        [XmlElement("description")] public string Description { get; set; }
        [XmlElement("version")] public string Version { get; set; }
        [XmlElement("author")] public string Authors { get; set; }
        [XmlElement("container")] public List<ModContainerInfo> Containers { get; set; }

        [XmlIgnore] public string RootDir { get; set; }
        [XmlIgnore] public string FilesDir { get; set; }

        // Serialization constructor
        public ModInfo()
        {
        }

        public ModInfo( ModInfo other )
        {
            Title = other.Title;
            Description = other.Description;
            Version = other.Version;
            Authors = other.Authors;
            Containers = new List<ModContainerInfo>( other.Containers.Count );
            for ( int i = 0; i < Containers.Count; i++ )
                Containers[i] = new ModContainerInfo( other.Containers[i] );
        }

        public static ModInfo CreateDefaultForDirectory( string directory )
        {
            var modInfo = new ModInfo
            {
                Title = Path.GetFileName( directory ),
                Description = string.Empty,
                Version = string.Empty,
                Authors = string.Empty,
                Containers = new List<ModContainerInfo>(),
                RootDir = directory,
                FilesDir = Path.Combine( directory, "files" )
            };
            return modInfo;
        }

        public static bool TryLoad(string path, out ModInfo modInfo)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(ModInfo));
                using ( var file = File.OpenRead( path ) )
                {
                    modInfo = ( ModInfo )serializer.Deserialize( file );
                    modInfo.RootDir = Path.GetDirectoryName( Path.GetFullPath( path ) );
                    modInfo.FilesDir = Path.Combine( modInfo.RootDir, "files" );
                }
                return true;
            }
            catch ( System.Exception )
            {
                modInfo = null;
                return false;
            }
        }

        public void Save( string path )
        {
            var serializer = new XmlSerializer(typeof(ModInfo));
            using ( var file = File.OpenWrite( path ) )
                serializer.Serialize( file, this );
        }
    }

    public class ModContainerInfo
    {
        public ModContainerInfo()
        {

        }

        public ModContainerInfo( ModContainerInfo other )
        {
            Path = other.Path;
            Merge = other.Merge;
        }

        [XmlAttribute( "path" )] public string Path { get; set; }
        [XmlAttribute( "merge" )] public bool Merge { get; set; }
    }
}
