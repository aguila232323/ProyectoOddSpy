using App.Model;
using Microsoft.Maui.Controls;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace App.View;

public partial class SureBetsContentPage : ContentPage
{
	public SureBetsContentPage()
	{
		InitializeComponent();
        LoadData();
    }
    private async void LoadData()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("datos.json");
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        var matches = JsonSerializer.Deserialize<List<Matches>>(json);
        collectionView.ItemsSource = matches;
    }
}