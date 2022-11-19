#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;
using System.Drawing;
using System;
using System.Drawing.Imaging;
using UnityEditor;


namespace Zanthous 
{
	public enum TilemapImageFormat
	{
		png,
		bmp,
		jpg
	}


	[CustomEditor(typeof(TilemapToImage))]
	public class TilemapToImageEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			TilemapToImage tilemapToPng = (TilemapToImage) target;

			if(GUILayout.Button("Generate"))
			{
				tilemapToPng.GenerateSprite();
			}
		}
	}


	public class TilemapToImage : MonoBehaviour
	{
		[SerializeField] string fileName = "";
		[SerializeField] bool compressBounds = true;
		[SerializeField] int pixelResolution = 32;
		[SerializeField] TilemapImageFormat format;
		Tilemap tm;

		int minX = 0;
		int maxX = 0;
		int minY = 0;
		int maxY = 0;

		public Bitmap bmp;

		Color32[] colors;
		int ySize;
		int xSize;
		float width;
		float height;

		public void GenerateSprite()
		{
			tm = GetComponent<Tilemap>();

			if(compressBounds)
				tm.CompressBounds();

			maxX = tm.cellBounds.xMax;
			minX = tm.cellBounds.xMin;
			maxY = tm.cellBounds.yMax;
			minY = tm.cellBounds.yMin;

			width = pixelResolution;
			height = pixelResolution;

			bmp = new Bitmap((int) width * tm.size.x, (int) height * tm.size.y, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite,
				bmp.PixelFormat);

			xSize = bmp.Width;
			ySize = bmp.Height;

			//idk why this is abs'd but it was from microsoft documentation so whatever
			int nBytes = Math.Abs(bmpData.Stride) * bmp.Height;

			byte[] rgba = new byte[nBytes];

			IntPtr ptr = bmpData.Scan0;

			//assign the entire invisible image
			colors = new Color32[bmp.Width * bmp.Height];
			for(int i = 0; i < colors.Length; i++)
			{
				colors[i] = new Color32(0, 0, 0, 0);
			}

			//assign to each block its respective pixels
			for(int x = minX; x <= maxX; x++)
			{
				for(int y = minY; y <= maxY; y++)
				{
					if(tm.GetSprite(new Vector3Int(x, y, 0)) != null)
					{
						//map the pixels so that the minX = 0 y minY = 0
						var t = tm.GetTransformMatrix(new Vector3Int(x, y, 0));

						SetColors(
							(x - minX) * (int) width, (y - minY) * (int) height,
							(int) width,
							(int) height,
							GetCurrentSprite(tm.GetSprite(new Vector3Int(x, y, 0)), t).GetPixels32());
					}
				}
			}

			for(int i = 0; i < nBytes / 4; i++)
			{
				rgba[i * 4 + 0] = colors[i].b;
				rgba[i * 4 + 1] = colors[i].g;
				rgba[i * 4 + 2] = colors[i].r;
				rgba[i * 4 + 3] = colors[i].a;
			}

			System.Runtime.InteropServices.Marshal.Copy(rgba, 0, ptr, nBytes);
			bmp.UnlockBits(bmpData);
			
			ExportImage(fileName == "" ? name : fileName, format);
		}
		//Copy the color32 values into our temporary array (these will be copied over to the Bitmap afterward)
		void SetColors(int x, int y, int width, int height, Color32[] colors_in)
		{
			for(int i = 0; i < height; i++)
			{
				for(int j = 0; j < width; j++)
				{
					colors[(ySize - pixelResolution - y + i) * xSize + (j + x)] = colors_in[(height - i - 1) * width + j];
				}
			}
		}

		Texture2D GetCurrentSprite(Sprite sprite, Matrix4x4 m) //retrieve sprite for this tile and transform it if necessary 
		{
			var pixels = sprite.texture.GetPixels((int) sprite.textureRect.x,
											 (int) sprite.textureRect.y,
											 (int) sprite.textureRect.width,
											 (int) sprite.textureRect.height);

			Texture2D texture = new Texture2D((int) sprite.textureRect.width,
											 (int) sprite.textureRect.height);
			texture.SetPixels(pixels);

			//Rotate/flip the pixels of tiles that are rotated/flipped. Slow and potentially unnecessary calls of apply
			//but I don't know what the best way to do it is and I don't need it to be fast
			if(Mathf.Approximately(m.rotation.eulerAngles.z, -90.0f) || Mathf.Approximately(m.rotation.eulerAngles.z, 270.0f))
			{
				Texture2D temp = new Texture2D(texture.width, texture.height);
				for(int y = 0; y < temp.height; y++)
				{
					for(int x = 0; x < temp.width; x++)
					{
						temp.SetPixel(x, y, texture.GetPixel(temp.width - y - 1, x));
					}
				}
				texture.SetPixels32(temp.GetPixels32());
			}
			if(Mathf.Approximately(m.rotation.eulerAngles.z, 90.0f) || Mathf.Approximately(m.rotation.eulerAngles.z, -270.0f))
			{
				Texture2D temp = new Texture2D(texture.width, texture.height);
				for(int y = 0; y < temp.height; y++)
				{
					for(int x = 0; x < temp.width; x++)
					{
						temp.SetPixel(x, y, texture.GetPixel(y, temp.height - x - 1));
					}
				}
				texture.SetPixels32(temp.GetPixels32());
			}
			if(Mathf.Approximately(m.rotation.eulerAngles.z, 180.0f))
			{
				Texture2D temp = new Texture2D(texture.width, texture.height);
				for(int y = 0; y < temp.height; y++)
				{
					for(int x = 0; x < temp.width; x++)
					{
						temp.SetPixel(x, y, texture.GetPixel(temp.height - x - 1, temp.width - y - 1));
					}
				}
				texture.SetPixels32(temp.GetPixels32());
			}
			if(Mathf.Approximately(m.lossyScale.x, -1.0f) || Mathf.Approximately(m.rotation.eulerAngles.y, 180.0f))
			{
				Texture2D temp = new Texture2D(texture.width, texture.height);
				for(int y = 0; y < temp.height; y++)
				{
					for(int x = 0; x < temp.width; x++)
					{
						temp.SetPixel(x, y, texture.GetPixel(temp.width - x - 1, y));
					}
				}
				texture.SetPixels32(temp.GetPixels32());
			}

			return texture;
		}

		void ExportImage(string fileName, TilemapImageFormat format)
		{
			var dirPath = Application.dataPath + "/Tilemap Images/";
			if(!Directory.Exists(dirPath))
			{
				Directory.CreateDirectory(dirPath);
			}
			ImageFormat imageFormat = ImageFormat.Png;

			//add more here if you want
			switch(format)
			{
				case TilemapImageFormat.png:
					imageFormat = ImageFormat.Png;
					break;
				case TilemapImageFormat.bmp:
					imageFormat = ImageFormat.Bmp;
					break;
				case TilemapImageFormat.jpg:
					imageFormat = ImageFormat.Jpeg;
					break;
			}

			bmp.Save(dirPath + fileName + ".png", imageFormat);
			bmp = null;
		}
	}
}
#endif