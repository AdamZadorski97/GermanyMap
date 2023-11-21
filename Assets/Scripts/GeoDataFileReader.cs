using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//[Serializable]
public class SerializableGeoDate
{
    public int id { get; set; }
    public string bundesland_name { get; set; }
    public string bundesland_nutscode { get; set; }
    public string regierungsbezirk_name { get; set; }
    public string regierungsbezirk_nutscode_kreis { get; set; }
    public string kreisname_kreis { get; set; }
    public string kreis_typ { get; set; }
    public string kreis_nutscode { get; set; }
    public string gemeinde_name { get; set; }
    public string gemeinde_typ { get; set; }
    public string gemeinde_ags { get; set; }
    public string gemeinde_rs { get; set; }
    public string gemeinde_lat { get; set; }
    public string gemeinde_lon { get; set; }
    public string ort_id { get; set; }
    public string ort_name { get; set; }
    public string ort_lat { get; set; }
    public string ort_lon { get; set; }
    public int postleitzahl { get; set; }
    public string strasse_name { get; set; }
}

public class GeoDataFileReader : MonoBehaviour
{
    [SerializeField] private string csvFilePath;
    public static List<SerializableGeoDate> geoDates = new List<SerializableGeoDate>();

    private void Awake()
    {
        string csvFilePath = "Assets/StreamingAssets/2720_geodatendeutschland_3012_202309041.csv";
        string path = Path.Combine(Application.streamingAssetsPath, csvFilePath);

        List<SerializableGeoDate> records = ReadDataFromCSV(csvFilePath);
        geoDates = ReadDataFromCSV(csvFilePath);
    }

    private List<SerializableGeoDate> ReadDataFromCSV(string filePath)
    {
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
                            bundesland_name = dataParts[1],
                            regierungsbezirk_name = dataParts[2],
                            bundesland_nutscode = dataParts[3],
                            regierungsbezirk_nutscode_kreis = dataParts[4],
                            kreisname_kreis = dataParts[5],
                            kreis_typ = dataParts[6],
                            kreis_nutscode = dataParts[7],
                            gemeinde_name = dataParts[8],
                            gemeinde_typ = dataParts[9],
                            gemeinde_ags = dataParts[10],
                            gemeinde_rs = dataParts[11],
                            gemeinde_lat = dataParts[12],
                            gemeinde_lon = dataParts[13],
                            ort_id = dataParts[14],
                            ort_name = dataParts[15],
                            ort_lat = dataParts[16],
                            ort_lon = dataParts[17],
                            postleitzahl = int.Parse(dataParts[18]),
                            strasse_name = dataParts[19]
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