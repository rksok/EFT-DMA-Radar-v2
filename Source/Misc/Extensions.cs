﻿using SkiaSharp;
using System.Numerics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace eft_dma_radar
{
    /// <summary>
    /// Extension methods go here.
    /// </summary>
    public static class Extensions
    {
        #region Generic Extensions
        /// <summary>
        /// Restarts a timer from 0. (Timer will be started if not already running)
        /// </summary>
        public static void Restart(this System.Timers.Timer t)
        {
            t.Stop();
            t.Start();
        }

        /// <summary>
        /// Converts 'Degrees' to 'Radians'.
        /// </summary>
        public static double ToRadians(this float degrees)
        {
            return (Math.PI / 180) * degrees;
        }
        /// <summary>
        /// Converts 'Radians' to 'Degrees'.
        /// </summary>
        public static double ToDegrees(this float radians)
        {
            return (180 / Math.PI) * radians;
        }
        /// <summary>
        /// Converts 'Degrees' to 'Radians'.
        /// </summary>
        public static double ToRadians(this double degrees)
        {
            return (Math.PI / 180) * degrees;
        }
        /// <summary>
        /// Converts 'Radians' to 'Degrees'.
        /// </summary>
        public static double ToDegrees(this double radians)
        {
            return (180 / Math.PI) * radians;
        }

        /// <summary>
        /// Converts a 3d position into a 2d position based on the localplayers view matrix
        /// </summary>
        /// <returns></returns>
        public static bool WorldToScreen(Vector3 position, float width, float height, out Vector2 screenPos)
        {
            screenPos = new Vector2(0, 0);

            var viewMatrix = Memory.CameraManager.ViewMatrix;
            viewMatrix = Matrix4x4.Transpose(viewMatrix);

            var translationVector = new Vector3(viewMatrix.M41, viewMatrix.M42, viewMatrix.M43);
            var up = new Vector3(viewMatrix.M21, viewMatrix.M22, viewMatrix.M23);
            var right = new Vector3(viewMatrix.M11, viewMatrix.M12, viewMatrix.M13);

            var w = Vector3.Dot(translationVector, position) + viewMatrix.M44;

            if (w < 0.098f)
                return false;

            var y = Vector3.Dot(up, position) + viewMatrix.M24;
            var x = Vector3.Dot(right, position) + viewMatrix.M14;

            screenPos.X = (width / 2f) * (1f + x / w);
            screenPos.Y = (height / 2f) * (1f - y / w);

            return true;
        }
        #endregion

        #region GUI Extensions
        public static Dictionary<string, SKPaint> PlayerTypeTextPaints = new Dictionary<string, SKPaint>();
        public static Dictionary<string, SKPaint> PlayerTypeFlagTextPaints = new Dictionary<string, SKPaint>();
        public static Dictionary<string, SKColor> SKColors = new Dictionary<string, SKColor>();
        private static PaintColor.Colors DefaultPaintColor = new PaintColor.Colors { R = 0, G = 0, B = 0, A = 0 };

        /// <summary>
        /// Convert game position to 'Bitmap' Map Position coordinates.
        /// </summary>
        public static MapPosition ToMapPos(this System.Numerics.Vector3 vector, Map map)
        {
            return new MapPosition()
            {
                X = map.ConfigFile.X + (vector.X * map.ConfigFile.Scale),
                Y = map.ConfigFile.Y - (vector.Z * map.ConfigFile.Scale), // Invert 'Y' unity 0,0 bottom left, C# top left
                Height = vector.Y // Keep as float, calculation done later
            };
        }

        /// <summary>
        /// Gets 'Zoomed' map position coordinates.
        /// </summary>
        public static MapPosition ToZoomedPos(this MapPosition location, MapParameters mapParams)
        {
            return new MapPosition()
            {
                UIScale = mapParams.UIScale,
                X = (location.X - mapParams.Bounds.Left) * mapParams.XScale,
                Y = (location.Y - mapParams.Bounds.Top) * mapParams.YScale,
                Height = location.Height
            };
        }

        /// <summary>
        /// Ghetto helper method to get the Color from a PaintColor object by Key & return a new SKColor object based on it
        /// </summary>
        public static SKColor SKColorFromPaintColor(string key, byte alpha=0) {
            var col = Extensions.SKColors[key];

            if (alpha > 0)
                col = col.WithAlpha(alpha);

            return col;
        }

        /// <summary>
        /// Gets drawing paintbrush based on Player Type
        /// </summary>
        public static SKPaint GetEntityPaint(this Player player) {
            var basePaint = SKPaints.PaintBase.Clone();

            basePaint.Color = player.Type switch {
                // AI
                PlayerType.Boss => Extensions.SKColorFromPaintColor("Boss"),
                PlayerType.BossGuard => Extensions.SKColorFromPaintColor("BossGuard"),
                PlayerType.BossFollower => Extensions.SKColorFromPaintColor("BossFollower"),
                PlayerType.Raider => Extensions.SKColorFromPaintColor("Raider"),
                PlayerType.Rogue => Extensions.SKColorFromPaintColor("Rogue"),
                PlayerType.Cultist => Extensions.SKColorFromPaintColor("Cultist"),
                PlayerType.Scav => Extensions.SKColorFromPaintColor("Scav"),

                // Player
                PlayerType.PlayerScav => Extensions.SKColorFromPaintColor("PlayerScav"),
                PlayerType.LocalPlayer => Extensions.SKColorFromPaintColor("LocalPlayer"),
                PlayerType.Teammate => Extensions.SKColorFromPaintColor("Teammate"),
                PlayerType.BEAR => Extensions.SKColorFromPaintColor("BEAR"),
                PlayerType.USEC => Extensions.SKColorFromPaintColor("USEC"),
                PlayerType.Special => Extensions.SKColorFromPaintColor("Special"),

                // Event/Temporary
                PlayerType.FollowerOfMorana => Extensions.SKColorFromPaintColor("FollowerOfMorana"),
                PlayerType.Zombie => Extensions.SKColorFromPaintColor("Zombie"),

                // default to yellow
                _ => new SKColor(255, 0, 255, 255),
            };

            return basePaint;
        }

        /// <summary>
        /// Ghetto helper method to get the Color from a PaintColor object by Key & return a new Vector4 object based on it
        /// </summary>
        public static Vector4 Vector4FromPaintColor(string key)
        {
            var col = Program.Config.PaintColors[key];
            var r = (float)col.R / 255f;
            var g = (float)col.G / 255f;
            var b = (float)col.B / 255f;
            var a = (float)col.A / 255f;
            return new Vector4(r,g,b,a);
        }

        /// <summary>
        /// Determines the items paint color.
        /// </summary>
        public static SKPaint GetEntityPaint(LootableObject item)
        {
            var isFiltered = !item.Color.Equals(DefaultPaintColor);
            var paintToUse = SKPaints.LootPaint.Clone();

            if (item.RequiredByQuest)
                paintToUse.Color = Extensions.SKColorFromPaintColor("RequiredQuestItem");
            else if (isFiltered)
            {
                var col = item.Color;
                paintToUse.Color = new SKColor(col.R, col.G, col.B, col.A);
            }
            else if (item.Important)
                paintToUse.Color = Extensions.SKColorFromPaintColor("ImportantLoot");
            else
                paintToUse.Color = Extensions.SKColorFromPaintColor("RegularLoot");

            return paintToUse;
        }

        /// <summary>
        /// Determines the death marker paint color.
        /// </summary>
        public static SKPaint GetDeathMarkerPaint(LootCorpse corpse)
        {
            var isFiltered = !corpse.Color.Equals(DefaultPaintColor);
            var paintToUse = SKPaints.DeathMarker.Clone();

            if (isFiltered)
            {
                var col = corpse.Color;
                paintToUse.Color = new SKColor(col.R, col.G, col.B, col.A);
            }
            else if (corpse.Important)
                paintToUse.Color = Extensions.SKColorFromPaintColor("ImportantLoot");
            else
                paintToUse.Color = Extensions.SKColorFromPaintColor("DeathMarker");

            return paintToUse;
        }

        public static SKPaint GetDeathMarkerPaint()
        {
            var paintToUse = SKPaints.DeathMarker.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("DeathMarker");
            return paintToUse;
        }

        /// <summary>
        /// Determines grenade paint color.
        /// </summary>
        public static SKPaint GetEntityPaint(Grenade grenade)
        {
            var paintToUse = SKPaints.PaintGrenades.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("Grenades");
            return paintToUse;
        }

        /// <summary>
        /// Determines tripwire paint color.
        /// </summary>
        public static SKPaint GetEntityPaint(Tripwire tripwire)
        {
            var paintToUse = SKPaints.PaintTripwires.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("Tripwires");
            return paintToUse;
        }

        /// <summary>
        /// Determines the quest items paint color.
        /// </summary>
        public static SKPaint GetEntityPaint(QuestItem item)
        {
            var paintToUse = SKPaints.LootPaint.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("QuestItem");
            return paintToUse;
        }

        /// <summary>
        /// Determines the quest zone paint color.
        /// </summary>
        public static SKPaint GetEntityPaint(QuestZone zone)
        {
            var paintToUse = SKPaints.LootPaint.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("QuestZone");
            return paintToUse;
        }

        /// <summary>
        /// Determines the exfil paint color.
        /// </summary>
        public static SKPaint GetEntityPaint(Exfil exfil)
        {
            var paintToUse = SKPaints.LootPaint.Clone();
            paintToUse.Color = exfil.Status switch
            {
                ExfilStatus.Open => Extensions.SKColorFromPaintColor("ExfilActiveIcon"),
                ExfilStatus.Pending => Extensions.SKColorFromPaintColor("ExfilPendingIcon"),
                ExfilStatus.Closed => Extensions.SKColorFromPaintColor("ExfilClosedIcon"),
                _ => Extensions.SKColorFromPaintColor("ExfilClosedIcon"),
            };

            return paintToUse;
        }

        /// <summary>
        /// Determines the transit paint color.
        /// </summary>
        public static SKPaint GetEntityPaint(Transit transit)
        {
            var paintToUse = SKPaints.LootPaint.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("TransitIcon");
            //paintToUse.Color = transit.Status switch
            //{
            //    TransitStatus.Open => Extensions.SKColorFromPaintColor("TransitActiveIcon"),
            //    TransitStatus.Pending => Extensions.SKColorFromPaintColor("TransitPendingIcon"),
            //    TransitStatus.Closed => Extensions.SKColorFromPaintColor("TransitClosedIcon"),
            //    _ => Extensions.SKColorFromPaintColor("TransitClosedIcon"),
            //};

            return paintToUse;
        }

        /// <summary>
        /// Gets color of text based on Player Type
        /// </summary>
        public static SKColor GetTextColor(Player player)
        {
            return player.Type switch
            {
                // AI
                PlayerType.Boss => Extensions.SKColorFromPaintColor("Boss"),
                PlayerType.BossGuard => Extensions.SKColorFromPaintColor("BossGuard"),
                PlayerType.BossFollower => Extensions.SKColorFromPaintColor("BossFollower"),
                PlayerType.Raider => Extensions.SKColorFromPaintColor("Raider"),
                PlayerType.Rogue => Extensions.SKColorFromPaintColor("Rogue"),
                PlayerType.Cultist => Extensions.SKColorFromPaintColor("Cultist"),
                PlayerType.Scav => Extensions.SKColorFromPaintColor("Scav"),

                // Player
                PlayerType.PlayerScav => Extensions.SKColorFromPaintColor("PlayerScav"),
                PlayerType.LocalPlayer => Extensions.SKColorFromPaintColor("LocalPlayer"),
                PlayerType.Teammate => Extensions.SKColorFromPaintColor("Teammate"),
                PlayerType.BEAR => Extensions.SKColorFromPaintColor("BEAR"),
                PlayerType.USEC => Extensions.SKColorFromPaintColor("USEC"),
                PlayerType.Special => Extensions.SKColorFromPaintColor("Special"),

                // Event/Temporary
                PlayerType.FollowerOfMorana => Extensions.SKColorFromPaintColor("FollowerOfMorana"),
                PlayerType.Zombie => Extensions.SKColorFromPaintColor("Zombie"),

                // default to magenta
                _ => new SKColor(255, 0, 255, 255),
            };
        }

        /// <summary>
        /// Determines the loot items text color.
        /// </summary>
        public static SKPaint GetTextPaint(LootableObject item)
        {
            var isFiltered = !item.Color.Equals(DefaultPaintColor);
            var paintToUse = SKPaints.LootText.Clone();

            if (item.RequiredByQuest)
                paintToUse.Color = Extensions.SKColorFromPaintColor("RequiredQuestItem");
            else if (isFiltered)
            {
                var col = item.Color;
                paintToUse.Color = new SKColor(col.R, col.G, col.B, col.A);
            }
            else if (item.Important)
                paintToUse.Color = Extensions.SKColorFromPaintColor("ImportantLoot");
            else
                paintToUse.Color = Extensions.SKColorFromPaintColor("RegularLoot");

            return paintToUse;
        }

        public static SKPaint GetTextPaint(GearItem item)
        {
            var isFiltered = !item.Color.Equals(DefaultPaintColor);
            var paintToUse = SKPaints.LootText.Clone();

            if (isFiltered)
            {
                var col = item.Color;
                paintToUse.Color = new SKColor(col.R, col.G, col.B, col.A);
            }
            else if (item.Important)
                paintToUse.Color = Extensions.SKColorFromPaintColor("ImportantLoot");
            else
                paintToUse.Color = Extensions.SKColorFromPaintColor("RegularLoot");

            return paintToUse;
        }

        /// <summary>
        /// Determines the quest items text color.
        /// </summary>
        public static SKPaint GetTextPaint(QuestItem item)
        {
            var paintToUse = SKPaints.LootText.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("QuestItem");
            return paintToUse;
        }

        /// <summary>
        /// Determines the quest zones text color.
        /// </summary>
        public static SKPaint GetTextPaint(QuestZone zone)
        {
            var paintToUse = SKPaints.LootText.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("QuestZone");
            return paintToUse;
        }

        /// <summary>
        /// Determines the exfil text color.
        /// </summary>
        public static SKPaint GetTextPaint(Exfil exfil)
        {
            var paintToUse = SKPaints.LootText.Clone();
            paintToUse.Color = exfil.Status switch
            {
                ExfilStatus.Open => Extensions.SKColorFromPaintColor("ExfilActiveText"),
                ExfilStatus.Pending => Extensions.SKColorFromPaintColor("ExfilPendingText"),
                ExfilStatus.Closed => Extensions.SKColorFromPaintColor("ExfilClosedText"),
                _ => Extensions.SKColorFromPaintColor("ExfilClosedText"),
            };

            return paintToUse;
        }

        /// <summary>
        /// Determines the transit text color.
        /// </summary>
        public static SKPaint GetTextPaint(Transit transit)
        {
            var paintToUse = SKPaints.LootText.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("TransitText");
            //paintToUse.Color = transit.Status switch
            //{
            //    TransitStatus.Open => Extensions.SKColorFromPaintColor("TransitActiveText"),
            //    TransitStatus.Pending => Extensions.SKColorFromPaintColor("TransitPendingText"),
            //    TransitStatus.Closed => Extensions.SKColorFromPaintColor("TransitClosedText"),
            //    _ => Extensions.SKColorFromPaintColor("TransitClosedText"),
            //};

            return paintToUse;
        }

        /// <summary>
        /// Determines the text outline color.
        /// </summary>
        public static SKPaint GetTextOutlinePaint()
        {
            var paintToUse = SKPaints.TextBaseOutline.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("TextOutline");
            return paintToUse;
        }

        /// <summary>
        /// Gets Aimview drawing paintbrush based on Player Type.
        /// </summary>
        public static SKPaint GetAimviewPaint(this Player player)
        {
            var basePaint = SKPaints.PlayerAimviewPaint.Clone();

            basePaint.Color = player.Type switch {
                // AI
                PlayerType.Boss => Extensions.SKColorFromPaintColor("Boss"),
                PlayerType.BossGuard => Extensions.SKColorFromPaintColor("BossGuard"),
                PlayerType.BossFollower => Extensions.SKColorFromPaintColor("BossFollower"),
                PlayerType.Raider => Extensions.SKColorFromPaintColor("Raider"),
                PlayerType.Rogue => Extensions.SKColorFromPaintColor("Rogue"),
                PlayerType.Cultist => Extensions.SKColorFromPaintColor("Cultist"),
                PlayerType.FollowerOfMorana => Extensions.SKColorFromPaintColor("FollowerOfMorana"),
                PlayerType.Scav => Extensions.SKColorFromPaintColor("Scav"),

                // Player
                PlayerType.PlayerScav => Extensions.SKColorFromPaintColor("PlayerScav"),
                PlayerType.LocalPlayer => Extensions.SKColorFromPaintColor("LocalPlayer"),
                PlayerType.Teammate => Extensions.SKColorFromPaintColor("Teammate"),
                PlayerType.BEAR => Extensions.SKColorFromPaintColor("BEAR"),
                PlayerType.USEC => Extensions.SKColorFromPaintColor("USEC"),
                PlayerType.Special => Extensions.SKColorFromPaintColor("Special"),

                // default to yellow
                _ => new SKColor(255, 0, 255, 255),
            };

            return basePaint;
        }

        public static SKPaint GetAimviewPaint(this LootableObject item)
        {
            var isFiltered = !item.Color.Equals(DefaultPaintColor);
            var paintToUse = SKPaints.LootAimviewPaint.Clone();

            if (isFiltered)
            {
                var col = item.Color;
                paintToUse.Color = new SKColor(col.R, col.G, col.B, col.A);
            }
            else if (item.Important)
                paintToUse.Color = Extensions.SKColorFromPaintColor("ImportantLoot");
            else
                paintToUse.Color = Extensions.SKColorFromPaintColor("RegularLoot");

            return paintToUse;
        }

        public static SKPaint GetAimviewPaint(this QuestItem item)
        {
            var paintToUse = SKPaints.LootAimviewPaint.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("QuestItem");
            return paintToUse;
        }

        public static SKPaint GetAimviewPaint(this QuestZone zone)
        {
            var paintToUse = SKPaints.LootAimviewPaint.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("QuestZone");
            return paintToUse;
        }

        public static SKPaint GetAimviewPaint(this Tripwire tripwire)
        {
            var paintToUse = SKPaints.PaintTripwires.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("Tripwires");
            return paintToUse;
        }

        public static SKPaint GetAimviewTextPaint(this Transit transit)
        {
            var paintToUse = SKPaints.AimviewText.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("TransitText");

            return paintToUse;
        }

        public static SKPaint GetAimviewTextPaint(this Exfil exfil)
        {
            var paintToUse = SKPaints.AimviewText.Clone();
            paintToUse.Color = exfil.Status switch
            {
                ExfilStatus.Open => Extensions.SKColorFromPaintColor("ExfilActiveIcon"),
                ExfilStatus.Pending => Extensions.SKColorFromPaintColor("ExfilPendingIcon"),
                ExfilStatus.Closed => Extensions.SKColorFromPaintColor("ExfilClosedIcon"),
                _ => Extensions.SKColorFromPaintColor("ExfilClosedIcon"),
            };

            return paintToUse;
        }

        public static SKPaint GetAimviewTextPaint(this LootableObject item)
        {
            var isFiltered = !item.Color.Equals(DefaultPaintColor);
            var paintToUse = SKPaints.AimviewText.Clone();

            if (item.RequiredByQuest)
                paintToUse.Color = Extensions.SKColorFromPaintColor("RequiredQuestItem");
            else if (isFiltered)
            {
                var col = item.Color;
                paintToUse.Color = new SKColor(col.R, col.G, col.B, col.A);
            }
            else if (item.Important)
                paintToUse.Color = Extensions.SKColorFromPaintColor("ImportantLoot");
            else
                paintToUse.Color = Extensions.SKColorFromPaintColor("RegularLoot");

            return paintToUse;
        }

        public static SKPaint GetAimviewTextPaint(this Player player)
        {
            var paintToUse = SKPaints.AimviewText.Clone();

            paintToUse.Color = player.Type switch
            {
                // AI
                PlayerType.Boss => Extensions.SKColorFromPaintColor("Boss"),
                PlayerType.BossGuard => Extensions.SKColorFromPaintColor("BossGuard"),
                PlayerType.BossFollower => Extensions.SKColorFromPaintColor("BossFollower"),
                PlayerType.Raider => Extensions.SKColorFromPaintColor("Raider"),
                PlayerType.Rogue => Extensions.SKColorFromPaintColor("Rogue"),
                PlayerType.Cultist => Extensions.SKColorFromPaintColor("Cultist"),
                PlayerType.FollowerOfMorana => Extensions.SKColorFromPaintColor("FollowerOfMorana"),
                PlayerType.Scav => Extensions.SKColorFromPaintColor("Scav"),

                // Player
                PlayerType.PlayerScav => Extensions.SKColorFromPaintColor("PlayerScav"),
                PlayerType.LocalPlayer => Extensions.SKColorFromPaintColor("LocalPlayer"),
                PlayerType.Teammate => Extensions.SKColorFromPaintColor("Teammate"),
                PlayerType.BEAR => Extensions.SKColorFromPaintColor("BEAR"),
                PlayerType.USEC => Extensions.SKColorFromPaintColor("USEC"),
                PlayerType.Special => Extensions.SKColorFromPaintColor("Special"),

                // default to yellow
                _ => new SKColor(255, 0, 255, 255),
            };

            return paintToUse;
        }

        public static SKPaint GetAimviewTextPaint(this QuestItem item)
        {
            var paintToUse = SKPaints.AimviewText.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("QuestItem");
            return paintToUse;
        }

        public static SKPaint GetAimviewTextPaint(this QuestZone zone)
        {
            var paintToUse = SKPaints.AimviewText.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("QuestZone");
            return paintToUse;
        }

        public static SKPaint GetAimviewTextPaint(this Tripwire tripwire)
        {
            var paintToUse = SKPaints.AimviewText.Clone();
            paintToUse.Color = Extensions.SKColorFromPaintColor("Tripwires");
            return paintToUse;
        }

        /// <summary>
        /// Get Exfil drawing paintbrush based on status.
        /// </summary>
        public static SKPaint GetPaint(this ExfilStatus status)
        {
            return status switch
            {
                ExfilStatus.Open => SKPaints.PaintExfilOpen,
                ExfilStatus.Pending => SKPaints.PaintExfilPending,
                _ => SKPaints.PaintExfilClosed
            };
        }
        #endregion
    }
}
