using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using MixedReality.Toolkit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CSVis.Tests
{
    /// <summary>
    /// Drives the real menu the way a tap would (StatefulInteractable.OnClicked) and exercises the failure paths.
    /// The app code lives in Assembly-CSharp, which test assemblies can't reference, so a few calls go through reflection.
    /// </summary>
    public class CSVisPlayModeTests
    {
        private const string PlotTag = "plotContainer";

        // ---- reflection into Assembly-CSharp
        private static Type Type_(string name) => Type.GetType(name + ", Assembly-CSharp", true);

        private static object PlotCsv(string file, string text)
        {
            return Type_("CSVis.Core.PlotCommands").GetMethod("PlotCsv").Invoke(null, new object[] { file, text });
        }

        private static bool Success(object result) => (bool)result.GetType().GetField("Success").GetValue(result);

        private static string Message(object result) => (string)result.GetType().GetField("Message").GetValue(result);

        private static int PlotCount() => GameObject.FindGameObjectsWithTag(PlotTag).Length;

        [UnitySetUp]
        public IEnumerator LoadMenuScene()
        {
            SceneManager.LoadScene("MenuScene");
            yield return null;
            yield return new WaitForSeconds(0.4f);
            Type_("CSVis.Core.Notifier").GetMethod("Clear").Invoke(null, null);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            yield return null;
        }

        // ------------------------------------------------------------------ menu

        [UnityTest]
        public IEnumerator HomeScreenShowsPrimaryActions()
        {
            Assert.IsNotNull(Camera.main, "MRTK XR Rig should provide the main camera");
            Assert.IsNotNull(Button("OpenFile"));
            Assert.IsNotNull(Button("Examples"));
            Assert.IsNotNull(Button("ClearAll"));
            Assert.IsNull(Button("Back"), "Back is hidden on the home screen");
            Assert.IsFalse(Button("ClearAll").enabled, "Clear all is disabled when there are no plots");
            yield break;
        }

        [UnityTest]
        public IEnumerator NavigatesAndGoesBackHome()
        {
            yield return Click("Examples");
            yield return Click("Static");
            Assert.IsNotNull(Button("Iris"));
            Assert.IsNotNull(Button("Back"));

            yield return Click("Back");
            Assert.IsNotNull(Button("Static"), "Back returns to Examples");
            Assert.IsNull(Button("Iris"));

            yield return Click("Dynamic");
            yield return Click("Home");
            Assert.IsNotNull(Button("OpenFile"), "Home returns to the start");
            Assert.IsNull(Button("Home"));
        }

        [UnityTest]
        public IEnumerator PinAndMinimizeWork()
        {
            var menu = GameObject.Find("CSVis Menu");
            Assert.IsNotNull(menu);

            yield return Click("Minimize");
            Assert.IsNotNull(Button("Maximize"), "Minimising shows the logo button");
            Assert.IsNull(Button("OpenFile"));

            yield return Click("Maximize");
            Assert.IsNotNull(Button("OpenFile"));

            var before = menu.transform.position;
            yield return Click("Pin");
            Camera.main.transform.position += Vector3.right * 3f;
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(before.x, menu.transform.position.x, 0.01f, "A pinned menu stays where it is");
        }

        // ------------------------------------------------------------------ examples

        [UnityTest]
        public IEnumerator EveryOfflineExampleSpawnsAPlot()
        {
            yield return Click("Examples");
            yield return Click("Static");
            foreach (var key in new[] { "SineWave", "Iris", "Electron" })
            {
                var before = PlotCount();
                yield return Click(key);
                Assert.Greater(PlotCount(), before, key);
            }

            yield return Click("Home");
            yield return Click("Examples");
            yield return Click("Dynamic");
            foreach (var key in new[] { "DynSine", "DynGeo", "DynDrone" })
            {
                var before = PlotCount();
                yield return Click(key);
                Assert.Greater(PlotCount(), before, key);
            }

            yield return new WaitForSeconds(1.5f);
        }

        [UnityTest, Category("Network")]
        public IEnumerator MapExamplesSpawn()
        {
            yield return Click("Examples");
            yield return Click("Maps");
            foreach (var key in new[] { "MapCarleton", "MapDrone", "MapParliament" })
            {
                var before = PlotCount();
                yield return Click(key);
                Assert.Greater(PlotCount(), before, key);
            }

            yield return new WaitForSeconds(4f);
        }

        [UnityTest]
        public IEnumerator PlotsAreSpreadOutSoTheyDoNotOverlap()
        {
            yield return Click("Examples");
            yield return Click("Static");
            yield return Click("SineWave");
            yield return Click("Iris");
            yield return Click("Electron");

            var holders = GameObject.FindGameObjectsWithTag(PlotTag);
            Assert.AreEqual(3, holders.Length);
            for (var i = 0; i < holders.Length; i++)
            {
                for (var j = i + 1; j < holders.Length; j++)
                {
                    Assert.Greater(Vector3.Distance(holders[i].transform.position, holders[j].transform.position), 0.3f);
                }
            }
        }

        [UnityTest]
        public IEnumerator PlotsAreCappedAtEight()
        {
            yield return Click("Examples");
            yield return Click("Static");
            for (var i = 0; i < 10; i++)
            {
                yield return Click("SineWave");
            }

            Assert.AreEqual(8, PlotCount());
            Assert.AreEqual("Warning", LastNotificationLevel());
        }

        [UnityTest]
        public IEnumerator PlotToolbarDeletesAndRecenters()
        {
            yield return Click("Examples");
            yield return Click("Static");
            yield return Click("SineWave");
            yield return new WaitForSeconds(0.3f);

            var holder = GameObject.FindGameObjectsWithTag(PlotTag).First();
            Assert.IsNotNull(holder.GetComponent("ObjectManipulator"), "Plots are grabbable");
            Assert.IsNotNull(holder.GetComponent("BoundsControl"), "Plots show bounds handles");

            holder.transform.position += Vector3.up * 2f;
            yield return Click("PlotRecenter");
            Assert.Less(holder.transform.position.y, Camera.main.transform.position.y + 0.5f);

            yield return Click("PlotDelete");
            yield return null;
            Assert.AreEqual(0, PlotCount());
        }

        [UnityTest]
        public IEnumerator DeletedDynamicPlotLeavesNoErrors()
        {
            yield return Click("Examples");
            yield return Click("Dynamic");
            yield return Click("DynSine");
            yield return new WaitForSeconds(0.5f);
            yield return Click("PlotDelete");
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(0, PlotCount());
        }

        [UnityTest]
        public IEnumerator ClearAllNeedsConfirmation()
        {
            yield return Click("Examples");
            yield return Click("Static");
            yield return Click("SineWave");
            yield return Click("Iris");
            yield return Click("Home");

            yield return Click("ClearAll");
            Assert.AreEqual(2, PlotCount(), "First tap only asks for confirmation");

            yield return Click("ClearAll");
            yield return null;
            Assert.AreEqual(0, PlotCount());
        }

        // ------------------------------------------------------------------ user CSV files

        [UnityTest]
        public IEnumerator Plots2DAnd3DCsvFiles()
        {
            Assert.IsTrue(Success(PlotCsv("two.csv", "x,y\n1,2\n2,4\n3,9\n")));
            Assert.IsTrue(Success(PlotCsv("three.csv", "x,y,z\n1,2,3\n2,4,1\n3,9,2\n")));
            yield return null;
            Assert.AreEqual(2, PlotCount());
        }

        [UnityTest]
        public IEnumerator PlotsAnimatedCsvFile()
        {
            Assert.IsTrue(Success(PlotCsv("move.csv", "t,x,y,z,x2,y2,z2\n0,0,0,0,1,1,1\n1,1,1,1,2,2,2\n2,2,2,2,3,3,3\n")));
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, PlotCount());
        }

        [UnityTest]
        public IEnumerator BadFilesGiveFriendlyErrorsAndNoPlots()
        {
            var cases = new[]
            {
                new[] { "empty.csv", "" },
                new[] { "blank.csv", "\n\n  \n" },
                new[] { "one.csv", "x\n1\n2\n" },
                new[] { "text.csv", "name,city\nBob,Paris\nAmy,Rome\n" },
                new[] { "five.csv", "a,b,c,d,e\n1,2,3,4,5\n" },
                new[] { "headeronly.csv", "x,y\n" },
                new[] { "binary.csv", "x,y\0\n1,2\n" },
            };

            foreach (var c in cases)
            {
                var result = PlotCsv(c[0], c[1]);
                Assert.IsFalse(Success(result), c[0]);
                Assert.IsNotEmpty(Message(result), c[0]);
            }

            yield return null;
            Assert.AreEqual(0, PlotCount(), "Failures must not leave half-built plots behind");
            Assert.AreEqual("Error", LastNotificationLevel());
        }

        [UnityTest]
        public IEnumerator PartiallyBadFilesPlotWhatTheyCanAndWarn()
        {
            var result = PlotCsv("messy.csv", "x,y\n1,2\nabc,4\n3,\n5,6\n7,8\n");
            Assert.IsTrue(Success(result));
            StringAssert.Contains("skipped", Message(result));
            yield return null;
            Assert.AreEqual(1, PlotCount());
            Assert.AreEqual("Warning", LastNotificationLevel());
        }

        [UnityTest]
        public IEnumerator ConstantColumnsDoNotProduceNaN()
        {
            Assert.IsTrue(Success(PlotCsv("flat.csv", "x,y\n5,5\n5,5\n5,5\n")));
            yield return null;
            var holder = GameObject.FindGameObjectsWithTag(PlotTag).First();
            foreach (var t in holder.GetComponentsInChildren<Transform>())
            {
                Assert.IsFalse(float.IsNaN(t.position.x) || float.IsNaN(t.position.y) || float.IsNaN(t.position.z), t.name);
            }
        }

        [UnityTest]
        public IEnumerator MapFileWithBadCoordinatesIsRejected()
        {
            var result = PlotCsv("map.csv", "lat,lon,h,r,g,b\n450,10,1,1,0,0\n");
            Assert.IsFalse(Success(result));
            yield return null;
            Assert.AreEqual(0, PlotCount());
        }

        [UnityTest]
        public IEnumerator HugeFilesAreTruncatedNotCrashed()
        {
            var sb = new System.Text.StringBuilder("x,y\n");
            for (var i = 0; i < 20000; i++)
            {
                sb.Append(i).Append(',').Append(i % 50).Append('\n');
            }

            var result = PlotCsv("big.csv", sb.ToString());
            Assert.IsTrue(Success(result));
            StringAssert.Contains("first 5,000", Message(result).Replace("5000", "5,000"));
            yield return null;
        }

        // ------------------------------------------------------------------ screenshots

        [UnityTest, Category("Screenshot")]
        public IEnumerator CaptureScreenshots()
        {
            var dir = Environment.GetEnvironmentVariable("CSVIS_SCREENSHOT_DIR");
            if (string.IsNullOrEmpty(dir))
            {
                Assert.Ignore("CSVIS_SCREENSHOT_DIR not set");
            }

            yield return Capture(dir, "1_home.png");
            yield return Click("Examples");
            yield return Capture(dir, "2_examples.png");
            yield return Click("Static");
            yield return Capture(dir, "3_static.png");
            yield return Click("Iris");
            yield return new WaitForSeconds(0.5f);
            yield return Capture(dir, "4_iris.png");
            PlotCsv("bad.csv", "name,city\nBob,Paris\n");
            yield return new WaitForSeconds(2.6f);
            yield return Capture(dir, "5_error_toast.png");
            yield return Click("Home");
            yield return Click("ClearAll");
            yield return Capture(dir, "6_confirm_clear.png");
        }

        private static IEnumerator Capture(string dir, string file)
        {
            // Give the font atlas and the menu solver a moment to settle before reading pixels
            yield return new WaitForSeconds(0.4f);
            var camera = Camera.main;
            var rt = new RenderTexture(1600, 900, 24) { antiAliasing = 4 };
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            camera.targetTexture = null;
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, file), tex.EncodeToPNG());
            UnityEngine.Object.Destroy(rt);
            UnityEngine.Object.Destroy(tex);
        }

        // ------------------------------------------------------------------ helpers

        private static string LastNotificationLevel()
        {
            var last = Type_("CSVis.Core.Notifier").GetProperty("Last").GetValue(null);
            if (last == null)
            {
                return "None";
            }

            // A boxed Nullable<Notification> is the Notification itself
            return last.GetType().GetField("Level").GetValue(last).ToString();
        }

        /// <summary>Finds a button by name among the visible (active) ones.</summary>
        private static StatefulInteractable Button(string name)
        {
            return UnityEngine.Object.FindObjectsByType<StatefulInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == name);
        }

        private static IEnumerator Click(string name)
        {
            var button = Button(name);
            Assert.IsNotNull(button, "Visible button '" + name + "' not found");
            button.OnClicked.Invoke();
            yield return null;
            yield return new WaitForSeconds(0.15f);
        }
    }
}
