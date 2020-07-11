using RLNET;
using System;

namespace RuneRogue.Core
{
    class ColorNotDefinedException : Exception
    {
        public ColorNotDefinedException(string message)
        {
        }
    }

    public class Colors
    {
        public static RLColor FloorBackground = RLColor.Black;
        public static RLColor Floor = Swatch.AlternateDarkest;
        public static RLColor FloorBackgroundFov = Swatch.DbDark;
        public static RLColor FloorFov = Swatch.Alternate;

        public static RLColor WallBackground = Swatch.SecondaryDarkest;
        public static RLColor Wall = Swatch.Secondary;
        public static RLColor WallBackgroundFov = Swatch.SecondaryDarker;
        public static RLColor WallFov = Swatch.SecondaryLighter;

        public static RLColor DoorBackground = Swatch.ComplimentDarkest;
        public static RLColor Door = Swatch.ComplimentLighter;
        public static RLColor DoorBackgroundFov = Swatch.ComplimentDarker;
        public static RLColor DoorFov = Swatch.ComplimentLightest;

        public static RLColor TextHeading = RLColor.White;
        public static RLColor Text = Swatch.DbLight;
        public static RLColor Gold = Swatch.DbSun;

        public static RLColor Player = Swatch.DbLight;
        public static RLColor KoboldColor = Swatch.DbBrightWood;
        public static RLColor BeetleColor = Swatch.DbGrass;
        public static RLColor DragonColor = Swatch.DbBlood;
        public static RLColor BoneClawsColor = Swatch.DbOldStone;

        public static RLColor ColorLookup(string color)
        {
            switch (color)
            {
                case "DbDark":
                    return Swatch.DbDark;
                case "DbBrightWood":
                    return Swatch.DbBrightWood;
                case "DbGrass":
                    return Swatch.DbGrass;
                case "DbBlood":
                    return Swatch.DbBlood;
                case "DbOldStone":
                    return Swatch.DbOldStone;
                case "DbDeepWater":
                    return Swatch.DbDeepWater;
                case "DbWood":
                    return Swatch.DbWood;
                case "DbVegetation":
                    return Swatch.DbVegetation;
                case "DbOldBlood":
                    return Swatch.DbOldBlood;
                case "DbStone":
                    return Swatch.DbStone;
                case "DbWater":
                    return Swatch.DbWater;
                case "DbMetal":
                    return Swatch.DbMetal;
                case "DbSkin":
                    return Swatch.DbSkin;
                case "DbSky":
                    return Swatch.DbSky;
                case "DbSun":
                    return Swatch.DbSun;
                case "DbLight":
                    return Swatch.DbLight;
                case "DwReddishGray":
                    return Swatch.DwReddishGray;
                case "DwForestGreen":
                    return Swatch.DwForestGreen;
                case "DwPurple":
                    return Swatch.DwPurple;
                default:
                    throw new ColorNotDefinedException($"{color} not defined in ColorLookup.");
            }
        }


    }

}
