using System.Collections.Generic;
using System.IO;

namespace GE2D3D.MapEditor.Data
{
    public class LevelInfo
    {
        public string Name { get; }
        public string MusicLoop { get; }
        public bool WildPokemonFloor { get; }
        public bool ShowOverworldPokemon { get; }
        public string CurrentRegion { get; }
        public int HiddenAbilityChance { get; }
        public bool CanTeleport { get; set; }
        public bool CanDig { get; set; }
        public bool CanFly { get; set; }
        public int RideType { get; set; }
        public int EnvironmentType { get; set; }
        public int WeatherType { get; set; }
        public int LightingType { get; set; }
        public bool IsDark { get; set; }
        public bool IsSafariZone { get; set; }
        public bool IsBugCatchingContest { get; set; }
        public string BugCatchingContestData { get; set; }
        public string MapScript { get; set; }
        public string BattleMapData { get; set; }
        public string SurfingBattleMapData { get; set; }

        public List<EntityInfo> Entities { get; }
        public List<EntityNPCInfo> NPCs { get; }
        public List<StructureInfo> Structures { get; }
        public List<OffsetMapInfo> OffsetMaps { get; }
        public ShaderInfo Shader { get; }
        public BackdropInfo Backdrop { get; }
        public string Path { get; }

        public string DirectoryLocation => System.IO.Path.GetDirectoryName(Path) ?? string.Empty;

        public string ContentRoot
        {
            get
            {
                var mapDirectory = new DirectoryInfo(DirectoryLocation);
                var mapsDirectory = mapDirectory;
                var dataDirectory = mapsDirectory.Parent;
                var contentDirectory = dataDirectory?.Parent;
                if (mapsDirectory.Name.Equals("maps", System.StringComparison.OrdinalIgnoreCase) &&
                    dataDirectory != null && dataDirectory.Name.Equals("Data", System.StringComparison.OrdinalIgnoreCase) &&
                    contentDirectory != null && contentDirectory.Name.Equals("Content", System.StringComparison.OrdinalIgnoreCase))
                    return contentDirectory.FullName;
                return DirectoryLocation;
            }
        }

        public string TexturesLocation
        {
            get
            {
                var p3dTextures = System.IO.Path.Combine(ContentRoot, "Textures");
                return Directory.Exists(p3dTextures) ? p3dTextures : System.IO.Path.Combine(DirectoryLocation, "Textures");
            }
        }

        public string StructuresLocation
        {
            get
            {
                var mapName = System.IO.Path.GetFileNameWithoutExtension(Path);
                var mapCompanionFolder = System.IO.Path.Combine(DirectoryLocation, mapName);
                if (Directory.Exists(mapCompanionFolder)) return mapCompanionFolder;
                var structuresFolder = System.IO.Path.Combine(DirectoryLocation, "Structures");
                return Directory.Exists(structuresFolder) ? structuresFolder : DirectoryLocation;
            }
        }

        public LevelInfo(LevelTags levelTags, string path, LevelTags actionTags, List<EntityInfo> entities, List<EntityNPCInfo> npcs, List<StructureInfo> structures, List<OffsetMapInfo> offsetMaps, ShaderInfo shader, BackdropInfo backdrop)
        {
            Name = levelTags.GetTag<string>("Name");
            MusicLoop = levelTags.GetTag<string>("MusicLoop");
            WildPokemonFloor = levelTags.TagExists("WildPokemon") && levelTags.GetTag<bool>("WildPokemon");
            ShowOverworldPokemon = !levelTags.TagExists("OverworldPokemon") || levelTags.GetTag<bool>("OverworldPokemon");
            CurrentRegion = levelTags.TagExists("CurrentRegion") ? levelTags.GetTag<string>("CurrentRegion") : "Johto";
            HiddenAbilityChance = levelTags.TagExists("HiddenAbility") ? levelTags.GetTag<int>("HiddenAbility") : 0;
            CanTeleport = actionTags.TagExists("CanTeleport") && actionTags.GetTag<bool>("CanTeleport");
            CanDig = actionTags.TagExists("CanDig") && actionTags.GetTag<bool>("CanDig");
            CanFly = actionTags.TagExists("CanFly") && actionTags.GetTag<bool>("CanFly");
            RideType = actionTags.TagExists("RideType") ? actionTags.GetTag<int>("RideType") : 0;
            EnvironmentType = actionTags.TagExists("EnviromentType") ? actionTags.GetTag<int>("EnviromentType") : 0;
            WeatherType = actionTags.TagExists("Weather") ? actionTags.GetTag<int>("Weather") : 0;
            LightingType = actionTags.TagExists("Lighting") ? actionTags.GetTag<int>("Lighting") : actionTags.TagExists("Lightning") ? actionTags.GetTag<int>("Lightning") : 1;
            IsDark = actionTags.TagExists("IsDark") && actionTags.GetTag<bool>("IsDark");
            IsSafariZone = actionTags.TagExists("IsSafariZone") && actionTags.GetTag<bool>("IsSafariZone");
            IsBugCatchingContest = actionTags.TagExists("BugCatchingContest");
            BugCatchingContestData = IsBugCatchingContest ? actionTags.GetTag<string>("BugCatchingContest") : "";
            MapScript = actionTags.TagExists("MapScript") ? actionTags.GetTag<string>("MapScript") : "";
            BattleMapData = actionTags.TagExists("BattleMap") ? actionTags.GetTag<string>("BattleMap") : "";
            SurfingBattleMapData = actionTags.TagExists("SurfingBattleMap") ? actionTags.GetTag<string>("SurfingBattleMap") : "";
            Entities = entities;
            NPCs = npcs;
            Structures = structures;
            OffsetMaps = offsetMaps;
            Shader = shader;
            Backdrop = backdrop;
            Path = path;
        }

        public override string ToString() => Name;
    }
}
