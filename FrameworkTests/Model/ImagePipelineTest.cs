﻿using FrameworkTests.DirectX;
using ImageFramework.DirectX;
using ImageFramework.ImageLoader;
using ImageFramework.Model;
using ImageFramework.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FrameworkTests.Model
{
    [TestClass]
    public class ImagePipelineTest
    {
        [TestMethod]
        public void NoImages()
        {
            var model = new Models(1);
            // nothing should happen and nothing should crash
            model.Apply();
        }

        [TestMethod]
        public void SingleImage()
        {
            var model = new Models(1);
            model.AddImageFromFile(TestData.Directory + "small.png");
            model.Apply();
            // look at the generated image
            Assert.IsNotNull(model.Pipelines[0].Image);
            var colors = model.Pipelines[0].Image.GetPixelColors(LayerMipmapSlice.Mip0);
            TestData.CompareWithSmall(colors, Color.Channel.Rgb);
        }

        [TestMethod]
        public void CombinedImages()
        {
            var model = new Models(1);
            model.AddImageFromFile(TestData.Directory + "checkers_left.png");
            model.AddImageFromFile(TestData.Directory + "checkers_right.png");
            model.Pipelines[0].Color.Formula = "I0 + I1";
            model.Apply();

            Assert.IsNotNull(model.Pipelines[0].Image);
            var colors = model.Pipelines[0].Image.GetPixelColors(LayerMipmapSlice.Mip0);
            TestData.TestCheckersLevel0(colors);
        }

        [TestMethod]
        public void GammaFilter()
        {
            var model = new Models(1);
            model.AddImageFromFile(TestData.Directory + "small.pfm");
            var filter = model.CreateFilter("filter/gamma.hlsl");
            // set factor to 4
            foreach (var param in filter.Parameters)
            {
                if (param.GetBase().Name == "Factor")
                {
                    param.GetFloatModel().Value = 4.0f;
                }
            }
            model.Filter.AddFilter(filter);

            model.Apply();
            var colors = model.Pipelines[0].Image.GetPixelColors(LayerMipmapSlice.Mip0);

            // compare with reference image
            var refColors = TestData.GetColors("smallx4.pfm");

            TestData.CompareColors(refColors, colors);
        }

        [TestMethod]
        public void MedianFilter()
        {
            var model = new Models(1);
            model.AddImageFromFile(TestData.Directory + "sphere_salt.png");
            model.Filter.AddFilter(model.CreateFilter("filter/median.hlsl"));
            model.Apply();
            var colors = model.Pipelines[0].Image.GetPixelColors(LayerMipmapSlice.Mip0);

            // compare with refence image
            var refColors = TestData.GetColors("sphere_median.png");

            TestData.CompareColors(refColors, colors);
        }

        [TestMethod]
        public void BlurFilter()
        {
            var model = new Models(1);
            model.AddImageFromFile(TestData.Directory + "sphere.png");
            model.Filter.AddFilter(model.CreateFilter("filter/blur.hlsl"));
            model.Apply();
            var colors = model.Pipelines[0].Image.GetPixelColors(LayerMipmapSlice.Mip0);

            // compare with refence image
            var refColors = TestData.GetColors("sphere_blur.png");

            TestData.CompareColors(refColors, colors);
        }

        [TestMethod]
        public void OptimizeTrivialFormula()
        {
            var model = new Models(1);
            model.AddImageFromFile(TestData.Directory + "small.png");
            model.AddImageFromFile(TestData.Directory + "small.pfm");
            model.Apply();

            Assert.IsTrue(ReferenceEquals(model.Pipelines[0].Image, model.Images.Images[0].Image));

            model.Pipelines[0].Color.Formula = "I1";
            model.Pipelines[0].Alpha.Formula = "I1";
            model.Apply();
            Assert.IsTrue(ReferenceEquals(model.Pipelines[0].Image, model.Images.Images[1].Image));
        }

        [TestMethod]
        public void Checkers3DInvert()
        {
            var model = new Models(1);
            model.AddImageFromFile(TestData.Directory + "checkers3d.dds");
            model.Apply();

            Assert.IsTrue(ReferenceEquals(model.Pipelines[0].Image, model.Images.Images[0].Image));
            TestData.TestCheckers3DLevel0(model.Pipelines[0].Image.GetPixelColors(LayerMipmapSlice.Mip0));
            TestData.TestCheckers3DLevel1(model.Pipelines[0].Image.GetPixelColors(LayerMipmapSlice.Mip1));
            TestData.TestCheckersLevel2(model.Pipelines[0].Image.GetPixelColors(LayerMipmapSlice.Mip2));

            model.Pipelines[0].Color.Formula = "1 - I0";
            model.Apply();
            Assert.IsNotNull(model.Pipelines[0].Image);

            var tex = model.Pipelines[0].Image;
            // compare with checkers texture
            TestData.TestCheckersLevel0Inverted(TestData.GetSlice(tex.GetPixelColors(LayerMipmapSlice.Mip0), 0 * 4 * 4, 4 * 4));
            TestData.TestCheckersLevel0Inverted(TestData.GetSlice(tex.GetPixelColors(LayerMipmapSlice.Mip0), 1 * 4 * 4, 4 * 4));
            TestData.TestCheckersLevel0(TestData.GetSlice(tex.GetPixelColors(LayerMipmapSlice.Mip0), 2 * 4 * 4, 4 * 4));
            TestData.TestCheckersLevel0(TestData.GetSlice(tex.GetPixelColors(LayerMipmapSlice.Mip0), 3 * 4 * 4, 4 * 4));
            
            TestData.TestCheckersLevel1Inverted(TestData.GetSlice(tex.GetPixelColors(LayerMipmapSlice.Mip1), 0, 2 * 2));
            TestData.TestCheckersLevel1(TestData.GetSlice(tex.GetPixelColors(LayerMipmapSlice.Mip1), 2 * 2, 2 * 2));
        }

        [TestMethod]
        public void Scale()
        {
            var model = new Models(1);
            model.AddImageFromFile(TestData.Directory + "small.png");
            model.Apply();

            Assert.IsFalse(model.Pipelines[0].HasChanges);

            model.Images.ScaleImages( new Size3(5, 6), model.Scaling);
            Assert.IsTrue(model.Pipelines[0].HasChanges);

            model.Apply();
            Assert.AreEqual(5, model.Images.Size.Width);
            Assert.AreEqual(6, model.Images.Size.Height);
            Assert.AreEqual(5, model.Pipelines[0].Image.Size.Width);
            Assert.AreEqual(6, model.Pipelines[0].Image.Size.Height);
        }

        [TestMethod]
        public void MipmapGeneration()
        {
            var model = new Models(1);
            model.AddImageFromFile(TestData.Directory + "checkers.dds");
            model.Pipelines[0].Color.Formula = "(I0+1)^2"; // nonlinear transformation on image
            model.Pipelines[0].RecomputeMipmaps = true;
            model.Apply();

            Assert.IsFalse(model.Pipelines[0].HasChanges);
            // check image data
            var mip1 = model.Pipelines[0].Image.GetPixelColors(LayerMipmapSlice.Mip1);
            Assert.AreEqual(4, mip1.Length);
            Assert.IsTrue(mip1[0].Equals(new Color(1.0f), Color.Channel.Rgb));
            Assert.IsTrue(mip1[1].Equals(new Color(4.0f) , Color.Channel.Rgb));
            Assert.IsTrue(mip1[2].Equals(new Color(4.0f), Color.Channel.Rgb));
            Assert.IsTrue(mip1[3].Equals(new Color(1.0f), Color.Channel.Rgb));

            // test if the highest mipmap was actually recalculated instead of averaged
            var mip2 = model.Pipelines[0].Image.GetPixelColors(LayerMipmapSlice.Mip2);
            Assert.AreEqual(1, mip2.Length);
            Assert.IsTrue(mip2[0].Equals(new Color(2.5f), Color.Channel.Rgb)); // average of all pixels should be ~2.5 
        }
    }
}
