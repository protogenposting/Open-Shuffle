using Godot;
﻿using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Godot.Collections;
using Emgu.CV.Dnn;
using System.Drawing;
using Emgu.CV;
using System.Collections.Generic;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.IO;
using Godot.NativeInterop;

public partial class Camera : Sprite3D
{
	String cameraName = "";
	CameraFeed camera;
	public override void _Ready()
	{
		foreach(CameraFeed feed in CameraServer.Feeds())
		{
			Dictionary temporary = new Dictionary
			{
				{ "output", "grayscale" }
			};

			feed.SetFormat(0, temporary);

			String name = feed.GetName();

			if(camera == null && (cameraName == "" || name == cameraName))
			{
				camera = feed;
			}
		}

		if(camera == null)
		{
			return;
		}

		camera.FeedIsActive = true;

		CameraTexture camTex = new CameraTexture();

		camTex.SetCameraFeedId(camera.GetId());

		Texture = camTex;
	}

    public override void _Process(double delta)
    {
        CameraTexture tex = (CameraTexture)Texture;

		Godot.Image image = tex.GetImage();

		godot_packed_byte_array data = image.GetData();

        Image<Bgr, byte> img = new Image<Bgr, byte>();

		try
		{
			// for openopse
			int inWidth = 368;
			int inHeight = 368;
			float threshold = 0.1f;
			int nPoints = 25;

			var BODY_PARTS = new System.Collections.Generic.Dictionary<string, int>()
			{
				{ "Nose", 0 },
				{ "Neck", 1 },
				{ "RShoulder", 2 },
				{ "RElbow", 3 },
				{ "RWrist", 4 },
				{"LShoulder",5},
				{ "LElbow", 6 },
				{ "LWrist", 7 },
				{ "MidHip", 8 },
				{ "RHip", 9 },
				{ "RKnee", 10 },
				{"RAnkle",11},
				{ "LHip", 12 },
				{ "LKnee", 13 },
				{ "LAnkle", 14 },
				{ "REye", 15 },
				{ "LEye", 16 },
				{"REar",17},
				{ "LEar", 18 },
				{ "LBigToe", 19 },
				{ "LSmallToe", 20 },
				{ "LHeel", 21 },
				{ "RBigToe", 22 },
				{"RSmallToe",23},
				{ "RHeel", 24 },
				{ "Background", 25 }
			};

			int[,] point_pairs = new int[,]{
						{1, 0}, {1, 2}, {1, 5},
						{2, 3}, {3, 4}, {5, 6},
						{6, 7}, {0, 15}, {15, 17},
						{0, 16}, {16, 18}, {1, 8},
						{8, 9}, {9, 10}, {10, 11},
						{11, 22}, {22, 23}, {11, 24},
						{8, 12}, {12, 13}, {13, 14},
						{14, 19}, {19, 20}, {14, 21}};


			// Load the caffe Model
			string prototxt = "res://models/pose.prototxt";

			var net = DnnInvoke.ReadNetFromCaffe(prototxt);

			var imgHeight = img.Height;
			var imgWidth = img.Width;

			var blob = DnnInvoke.BlobFromImage((IInputArray)img, 1.0 / 255.0, new Size(inWidth, inHeight), new MCvScalar(0, 0, 0));
			net.SetInput(blob);
			net.SetPreferableBackend(Emgu.CV.Dnn.Backend.OpenCV);

			var output = net.Forward();

			var H = output.SizeOfDimension[2];
			var W = output.SizeOfDimension[3];
			var HeatMap = output.GetData();

			var points = new List<Point>();

			for (int i = 0; i < nPoints; i++)
			{
				Matrix<float> matrix = new Matrix<float>(H, W);
				for (int row = 0; row < H; row++)
				{
					for (int col = 0; col < W; col++)
					{
						matrix[row, col] = (float)HeatMap.GetValue(0, i, row, col);
					}
				}

				double minVal = 0, maxVal = 0;
				Point minLoc = default, maxLoc = default;

				CvInvoke.MinMaxLoc(matrix, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

				var x = (img.Width * maxLoc.X) / W;
				var y = (img.Height * maxLoc.Y) / H;

				if (maxVal>threshold)
				{
					points.Add(new Point(x, y));
				}
				else
				{
					points.Add(Point.Empty);
				}
			}
		}
		catch (Exception ex)
		{
			GD.Print(ex.Message);
		}
    }
}
