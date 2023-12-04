using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SerializableGeoDate
{
    public int id;
    public string BUNDESLAND_NAME;
    public string BUNDESLAND_NUTSCODE;
    public string REGIERUNGSBEZIRK_NAME;
    public string REGIERUNGSBEZIRK_NUTSCODE;
    public string KREIS_NAME;
    public string KREIS_TYP;
    public string KREIS_NUTSCODE;
    public string GEMEINDE_NAME;
    public string GEMEINDE_TYP;
    public string GEMEINDE_AGS;
    public string GEMEINDE_RS;
    public string GEMEINDE_LAT;
    public string GEMEINDE_LON;
    public string ORT_ID;
    public string ORT_NAME;
    public string ORT_LAT;
    public string ORT_LON;
    public int POSTLEITZAHL;  
}

public class GeoDataFileReader : MonoBehaviour
{
    [SerializeField] private string csvFilePath;
    public List<SerializableGeoDate> geoDates = new List<SerializableGeoDate>();

    private void Awake()
    {
        string csvFilePath = "4673_geodatendeutschland_1001_20210723.csv";
        string path = Path.Combine(Application.streamingAssetsPath, csvFilePath);
        geoDates = ReadDataFromCSV(path);

       
    }


    private List<SerializableGeoDate> ReadDataFromCSV(string filePath)
    {
        Debug.Log("Try do read data from csv");
        List<SerializableGeoDate> records = new List<SerializableGeoDate>();

        try
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                string headerLine = reader.ReadLine();
                string[] headers = headerLine.Split(',');

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] dataParts = line.Split(',');

                    if (dataParts.Length == headers.Length)
                    {
                        SerializableGeoDate record = new SerializableGeoDate
                        {
                            id = int.Parse(dataParts[0]),
                            BUNDESLAND_NAME = dataParts[1],
                            BUNDESLAND_NUTSCODE = dataParts[2],
                            REGIERUNGSBEZIRK_NAME = dataParts[3],
                            REGIERUNGSBEZIRK_NUTSCODE = dataParts[4],
                            KREIS_NAME = dataParts[5],
                            KREIS_TYP = dataParts[6],
                            KREIS_NUTSCODE = dataParts[7],
                            GEMEINDE_NAME = dataParts[8],
                            GEMEINDE_TYP = dataParts[9],
                            GEMEINDE_AGS = dataParts[10],
                            GEMEINDE_RS = dataParts[11],
                            GEMEINDE_LAT = dataParts[12],
                            GEMEINDE_LON = dataParts[13],
                            ORT_ID = dataParts[14],
                            ORT_NAME = dataParts[15],
                            ORT_LAT = dataParts[16],
                            ORT_LON = dataParts[17],
                            POSTLEITZAHL = int.Parse(dataParts[18]),
                        };
                        records.Add(record);
                    }
                    else
                    {
                        Debug.Log($"Incorrect record format CSV: {line}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"File read error CSV: {ex.Message}");
        }

        return records;
    }
}