using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MixedReality.Toolkit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CSVis.Tests
{
    /// <summary>
    /// Records a scripted tour of the real app to numbered JPG frames plus a caption timeline, which the
    /// submission pipeline turns into the demo video. Only runs when CSVIS_VIDEO_DIR is set.
    /// </summary>
    public class DemoRecorder
    {
        private const int Fps = 30;
        private const int Width = 1920;
        private const int Height = 1080;

        private string dir;
        private int frame;
        private string caption = string.Empty;
        private readonly StringBuilder captions = new StringBuilder();
        private string lastCaption = string.Empty;
        private int captionStart;

        private RenderTexture rt;
        private Texture2D tex;
        private Camera camera;

        private Vector2 tapPoint;
        private int tapAge = -1;

        private Vector3 baseCamPos;
        private Quaternion baseCamRot;

        private static Type Type_(string name) => Type.GetType(name + ", Assembly-CSharp", true);

        [UnityTest, Category("Demo")]
        public IEnumerator RecordDemo()
        {
            dir = Environment.GetEnvironmentVariable("CSVIS_VIDEO_DIR");
            if (string.IsNullOrEmpty(dir))
            {
                Assert.Ignore("CSVIS_VIDEO_DIR not set");
            }

            Directory.CreateDirectory(Path.Combine(dir, "frames"));

            SceneManager.LoadScene("MenuScene");
            yield return null;
            yield return new WaitForSeconds(1f);

            Time.captureFramerate = Fps;
            camera = Camera.main;
            rt = new RenderTexture(Width, Height, 24) { antiAliasing = 4 };
            tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            camera.targetTexture = rt;
            baseCamPos = camera.transform.position;
            baseCamRot = camera.transform.rotation;

            // Keep the menu where it is while the camera moves around the plots
            var menuType = Type_("CSVis.UI.MenuController");
            var menu = menuType.GetProperty("Instance").GetValue(null);
            menuType.GetMethod("SetPinned").Invoke(menu, new object[] { true });

            // ---- Home
            Caption("CSVis: mixed reality data visualization for HoloLens 2");
            yield return Hold(2.8f);
            yield return Tap("Examples");

            // ---- Static plots
            Caption("Pick a plot from the floating menu. Everything is placed in your room.");
            yield return Hold(0.8f);
            yield return Tap("Static");
            yield return Hold(0.6f);
            yield return Tap("Iris");
            Caption("3D scatter plots you can grab, move, rotate and scale");
            yield return Orbit(5.5f, 0.28f);
            ClearPlots();
            yield return Tap("Electron");
            Caption("Thousands of points, rendered smoothly");
            yield return Orbit(4.5f, 0.28f);

            // ---- Dynamic plots
            ClearPlots();
            yield return Tap("Back");
            yield return Tap("Dynamic");
            yield return Hold(0.5f);
            yield return Tap("DynDrone");
            Caption("Animated plots: a real drone flight path, replayed in 3D");
            yield return Orbit(7.5f, 0.25f);

            // ---- Maps
            ClearPlots();
            yield return Tap("Back");
            yield return Tap("Maps");
            yield return Hold(0.5f);
            yield return Tap("MapCarleton");
            Caption("Geographic maps with live Mapbox tiles");
            yield return Orbit(8f, 0.22f);

            // ---- Own data and safety
            ClearPlots();
            yield return Tap("Home");
            Caption("Open your own CSV files: the layout is detected automatically");
            PlotCsv("sensor_readings.csv", BuildSampleCsv());
            yield return Hold(1.2f);
            yield return Orbit(4.5f, 0.25f);

            Caption("Bad data never crashes it: clear, friendly messages instead");
            PlotCsv("notes.csv", "name,city\nBob,Paris\nAmy,Rome\n");
            yield return Hold(3.2f);

            Caption("Safe by design: destructive actions ask twice");
            yield return Tap("ClearAll");
            yield return Hold(0.9f);
            yield return Tap("ClearAll");
            yield return Hold(2f);

            captions.AppendLine(captionStart + "|" + frame + "|" + lastCaption);
            File.WriteAllText(Path.Combine(dir, "captions.txt"), captions.ToString());
            File.WriteAllText(Path.Combine(dir, "frames.txt"), frame.ToString());
            Time.captureFramerate = 0;
            camera.targetTexture = null;
        }

        // ------------------------------------------------------------------ recording primitives

        private void Caption(string text)
        {
            caption = text;
        }

        private IEnumerator Hold(float seconds)
        {
            var count = Mathf.RoundToInt(seconds * Fps);
            for (var i = 0; i < count; i++)
            {
                yield return Capture();
            }
        }

        /// <summary>Holds while the camera glides side to side around the plots, so the 3D depth is visible.</summary>
        private IEnumerator Orbit(float seconds, float amplitude)
        {
            var count = Mathf.RoundToInt(seconds * Fps);
            var focus = FocusPoint();
            var right = baseCamRot * Vector3.right;
            for (var i = 0; i < count; i++)
            {
                var t = (float)i / count;
                var offset = Mathf.Sin(t * Mathf.PI * 2f) * amplitude;
                camera.transform.position = baseCamPos + right * offset;
                camera.transform.rotation = Quaternion.Slerp(baseCamRot,
                    Quaternion.LookRotation(focus - camera.transform.position, Vector3.up), 0.85f);
                yield return Capture();
            }

            camera.transform.SetPositionAndRotation(baseCamPos, baseCamRot);
        }

        private Vector3 FocusPoint()
        {
            var holders = GameObject.FindGameObjectsWithTag("plotContainer");
            if (holders.Length == 0)
            {
                return baseCamPos + baseCamRot * Vector3.forward;
            }

            var sum = Vector3.zero;
            foreach (var h in holders) sum += h.transform.position;
            return sum / holders.Length;
        }

        private IEnumerator Tap(string name)
        {
            var button = UnityEngine.Object.FindObjectsByType<StatefulInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == name);
            Assert.IsNotNull(button, "button " + name);

            var screen = camera.WorldToScreenPoint(button.transform.position);
            tapPoint = new Vector2(screen.x, screen.y);
            tapAge = 0;

            for (var i = 0; i < 14; i++)
            {
                if (i == 5)
                {
                    button.OnClicked.Invoke();
                }

                yield return Capture();
            }
        }

        private IEnumerator Capture()
        {
            yield return null;

            camera.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);

            if (tapAge >= 0)
            {
                DrawTap(tex, tapPoint, tapAge);
                tapAge++;
                if (tapAge > 12) tapAge = -1;
            }

            tex.Apply(false);
            File.WriteAllBytes(Path.Combine(dir, "frames", "f_" + frame.ToString("D5") + ".jpg"), tex.EncodeToJPG(92));
            RenderTexture.active = null;

            if (caption != lastCaption)
            {
                if (lastCaption.Length > 0)
                {
                    captions.AppendLine(captionStart + "|" + frame + "|" + lastCaption);
                }

                lastCaption = caption;
                captionStart = frame;
            }

            frame++;

        }

        /// <summary>A soft expanding ring and dot, like a fingertip pressing the button.</summary>
        private static void DrawTap(Texture2D target, Vector2 centre, int age)
        {
            var radius = 14f + age * 5f;
            var alpha = Mathf.Clamp01(1f - age / 12f);
            var accent = new Color(0.30f, 0.79f, 0.94f);
            var minX = Mathf.Max(0, (int)(centre.x - radius - 6));
            var maxX = Mathf.Min(target.width - 1, (int)(centre.x + radius + 6));
            var minY = Mathf.Max(0, (int)(centre.y - radius - 6));
            var maxY = Mathf.Min(target.height - 1, (int)(centre.y + radius + 6));
            var block = target.GetPixels(minX, minY, maxX - minX + 1, maxY - minY + 1);
            var w = maxX - minX + 1;

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), centre);
                    var ring = Mathf.Clamp01(1f - Mathf.Abs(d - radius) / 3.5f) * alpha;
                    var dot = d < 9f ? Mathf.Clamp01(1f - d / 9f) * alpha * 0.9f : 0f;
                    var a = Mathf.Max(ring, dot);
                    if (a <= 0f) continue;
                    var i = (y - minY) * w + (x - minX);
                    block[i] = Color.Lerp(block[i], accent, a);
                }
            }

            target.SetPixels(minX, minY, w, maxY - minY + 1, block);
        }

        // ------------------------------------------------------------------ helpers

        private static void ClearPlots()
        {
            Type_("CSVis.Core.PlotManager").GetMethod("ClearAll").Invoke(null, null);
        }

        private static void PlotCsv(string file, string text)
        {
            Type_("CSVis.Core.PlotCommands").GetMethod("PlotCsv").Invoke(null, new object[] { file, text });
        }

        private static string BuildSampleCsv()
        {
            // A small helix of sensor readings: x, y, z
            var sb = new StringBuilder("x,y,z\n");
            for (var i = 0; i < 400; i++)
            {
                var t = i * 0.08f;
                sb.Append((Mathf.Cos(t) * (1f + t * 0.08f)).ToString("F4", System.Globalization.CultureInfo.InvariantCulture)).Append(',');
                sb.Append((t * 0.25f).ToString("F4", System.Globalization.CultureInfo.InvariantCulture)).Append(',');
                sb.Append((Mathf.Sin(t) * (1f + t * 0.08f)).ToString("F4", System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
            }

            return sb.ToString();
        }
    }
}
