using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class TileDownloader: MonoBehaviour
{
    private string folder;

    private double leftLongitude;
    private double topLatitude;
    private double rightLongitude;
    private double bottomLatitude;
    private int minZoom = 3;
    private int maxZoom = 20;

    private const int averageSize = 30000;
    private int countTiles = 0;
    private long totalSize = 0;
    private static OnlineMaps map;
    private OnlineMapsDrawingRect rect;

    private List<Tile> downloadTiles;

    private void Calculate()
    {
        countTiles = 0;
        for (int z = minZoom; z <= maxZoom; z++)
        {
            double tlx, tly, brx, bry;
            map.projection.CoordinatesToTile(leftLongitude, topLatitude, z, out tlx, out tly);
            map.projection.CoordinatesToTile(rightLongitude, bottomLatitude, z, out brx, out bry);

            int itlx = (int) tlx;
            int itly = (int) tly;
            int ibrx = (int)Math.Ceiling(brx);
            int ibry = (int)Math.Ceiling(bry);

            countTiles += (ibrx - itlx) * (ibry - itly);
        }

        totalSize = countTiles * averageSize;
    }

    private double DoubleField(string label, double value)
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label(label, GUILayout.Width(150));
        string strVal = GUILayout.TextField(value.ToString());

        GUILayout.EndHorizontal();

        double newValue;
        if (double.TryParse(strVal, out newValue)) return newValue;
        return value;
    }

    private void Download()
    {
        downloadTiles = new List<Tile>();

        for (int z = minZoom; z <= maxZoom; z++)
        {
            double tlx, tly, brx, bry;
            map.projection.CoordinatesToTile(leftLongitude, topLatitude, z, out tlx, out tly);
            map.projection.CoordinatesToTile(rightLongitude, bottomLatitude, z, out brx, out bry);

            int itlx = (int)tlx;
            int itly = (int)tly;
            int ibrx = (int)Math.Ceiling(brx);
            int ibry = (int)Math.Ceiling(bry);

            for (int x = itlx; x < ibrx; x++)
            {
                for (int y = itly; y < ibry; y++)
                {
                    downloadTiles.Add(new Tile
                    {
                        x = x,
                        y = y,
                        zoom = z
                    });
                }
            }

            countTiles += (ibrx - itlx) * (ibry - itly);
        }

        StartNextDownload();
    }

    private string GetTilePath(Tile tile)
    {
        return GetTilePath(tile.zoom, tile.x, tile.y);
    }

    private string GetTilePath(int zoom, int x, int y)
    {
        StringBuilder builder = new StringBuilder(folder);
        builder.Append(zoom).Append("/");
        builder.Append(x).Append("/");
        builder.Append(y).Append(".png");

        return builder.ToString();
    }

    private int IntField(string label, int value)
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label(label, GUILayout.Width(150));
        string strVal = GUILayout.TextField(value.ToString());

        GUILayout.EndHorizontal();

        int newValue;
        if (int.TryParse(strVal, out newValue)) return newValue;
        return value;
    }

    private void OnGUI()
    {
        return;
        GUILayout.BeginArea(new Rect(10, 10, 300, Screen.height - 20));

        double newLeftLongitude = DoubleField("Left Longitude", leftLongitude);
        double newTopLatitude = DoubleField("Top Latitude", topLatitude);
        double newRightLongitude = DoubleField("Right Longitude", rightLongitude);
        double newBottomLatitude = DoubleField("Bottom Latitude", bottomLatitude);

        bool updateRect = false;

        if (Math.Abs(newLeftLongitude - leftLongitude) > double.Epsilon)
        {
            leftLongitude = newLeftLongitude;
            updateRect = true;
        }

        if (Math.Abs(newRightLongitude - rightLongitude) > double.Epsilon)
        {
            rightLongitude = newRightLongitude;
            updateRect = true;
        }

        if (Math.Abs(newBottomLatitude - bottomLatitude) > double.Epsilon)
        {
            bottomLatitude = newBottomLatitude;
            updateRect = true;
        }

        if (Math.Abs(newTopLatitude - topLatitude) > double.Epsilon)
        {
            topLatitude = newTopLatitude;
            updateRect = true;
        }

        if (updateRect) UpdateRect();

        minZoom = IntField("Min Zoom", minZoom);
        maxZoom = IntField("Max Zoom", maxZoom);

        if (GUILayout.Button("Place")) PlaceRect();
        if (GUILayout.Button("Calculate")) Calculate();

        GUILayout.Label("Count Tiles: " + countTiles);
        GUILayout.Label("Total Size: " + totalSize);

        if (GUILayout.Button("Download")) Download();

        GUILayout.EndArea();
    }

    private void OnStartDownloadTile(OnlineMapsTile tile)
    {
        string tilePath = GetTilePath(tile.zoom, tile.x, tile.y);
        if (!File.Exists(tilePath))
        {
            OnlineMapsTileManager.StartDownloadTile(tile);
            return;
        }

        byte[] bytes = File.ReadAllBytes(tilePath);

        Texture2D tileTexture = new Texture2D(256, 256);
        tileTexture.LoadImage(bytes);
        tileTexture.wrapMode = TextureWrapMode.Clamp;

        if (map.control.resultIsTexture)
        {
            (tile as OnlineMapsRasterTile).ApplyTexture(tileTexture);
            map.buffer.ApplyTile(tile);
            OnlineMapsUtils.Destroy(tileTexture);
        }
        else
        {
            tile.texture = tileTexture;
            tile.status = OnlineMapsTileStatus.loaded;
        }

        tile.MarkLoaded();

        map.Redraw();
    }

    private void OnTileDownloaded(OnlineMapsWWW www)
    {
        string tilePath = www["path"] as string;
        if (!www.hasError)
        {
            FileInfo fileInfo = new FileInfo(tilePath);
            DirectoryInfo directoryInfo = fileInfo.Directory;
            if (!directoryInfo.Exists) directoryInfo.Create();

            File.WriteAllBytes(tilePath, www.bytes);
        }

        StartNextDownload();
    }

    private void PlaceRect()
    {
        map.GetTileCorners(out leftLongitude, out topLatitude, out rightLongitude, out bottomLatitude, 20);
        double rx = rightLongitude - leftLongitude;
        double ry = bottomLatitude - topLatitude;
        double cx = (rightLongitude + leftLongitude) / 2;
        double cy = (bottomLatitude + topLatitude) / 2;
        rx *= 0.8;
        ry *= 0.8;

        leftLongitude = cx - rx / 2;
        rightLongitude = cx + rx / 2;
        topLatitude = cy - ry / 2;
        bottomLatitude = cy + ry / 2;

        map.projection.TileToCoordinates(leftLongitude, topLatitude, 20, out leftLongitude, out topLatitude);
        map.projection.TileToCoordinates(rightLongitude, bottomLatitude, 20, out rightLongitude, out bottomLatitude);

        UpdateRect();

        map.Redraw();
    }

    private void Start()
    {
        map = OnlineMaps.instance;

        folder = Application.persistentDataPath + "/Tiles/";

        if (OnlineMapsCache.instance != null) OnlineMapsCache.instance.OnStartDownloadTile += OnStartDownloadTile;
        else OnlineMapsTileManager.OnStartDownloadTile += OnStartDownloadTile;
    }

    private void StartNextDownload()
    {
        if (downloadTiles == null) return;

        while (downloadTiles.Count > 0)
        {
            Tile tile = downloadTiles[0];
            downloadTiles.RemoveAt(0);
            string tilePath = GetTilePath(tile);
            if (File.Exists(tilePath)) continue;

            string url = tile.url;
            Debug.Log(url + "    " + tilePath);
            OnlineMapsWWW www = new OnlineMapsWWW(url);
            www["path"] = tilePath;
            www.OnComplete += OnTileDownloaded;
            return;
        }
    }

    private void UpdateRect()
    {
        if (rect == null)
        {
            rect = new OnlineMapsDrawingRect((float) leftLongitude, (float) bottomLatitude, (float) (rightLongitude - leftLongitude), (float) (topLatitude - bottomLatitude), Color.blue, 5, new Color(1, 1, 1, 0.1f));
            OnlineMapsDrawingElementManager.AddItem(rect);
        }
        else
        {
            rect.x = leftLongitude;
            rect.y = bottomLatitude;
            rect.width = rightLongitude - leftLongitude;
            rect.height = topLatitude - bottomLatitude;
        }
    }

    private struct Tile
    {
        public int x, y, zoom;

        public string url
        {
            get
            {
                OnlineMapsRasterTile tile = new OnlineMapsRasterTile(x, y, zoom, map, false);
                string tileURL = map.activeType.GetURL(tile);
                tile.Dispose();
                return tileURL;
            }
        }
    }
}