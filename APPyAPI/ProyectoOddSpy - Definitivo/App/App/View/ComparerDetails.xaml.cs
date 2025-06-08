using App.Model;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace App.View;

[QueryProperty(nameof(SelectedMatchJson), "SelectedMatch")]
public partial class ComparerDetail : ContentPage
{
    private Matches _selectedMatch;
    private string selectedMatchJson;

    public string SelectedMatchJson
    {
        get => selectedMatchJson;
        set
        {
            selectedMatchJson = Uri.UnescapeDataString(value);
            SelectedMatch = JsonSerializer.Deserialize<Matches>(selectedMatchJson);
        }
    }

    public Matches SelectedMatch
    {
        get => _selectedMatch;
        set
        {
            _selectedMatch = value;
            OnPropertyChanged(nameof(SelectedMatch));
            OnPropertyChanged(nameof(Casino1Logo));
            OnPropertyChanged(nameof(CasinoXLogo));
            OnPropertyChanged(nameof(Casino2Logo));
        }
    }

    
    private static readonly Dictionary<string, string> CasinoLogoMap = new()
    {
        { "Interwetten", "logo_interwetten.png" },
        { "Bwin", "logo_bwin.png" },
        { "Betfair", "logo_betfair.png" },
        { "ApuestasAndalucia", "logo_apuestasandalucia.png" },
        
    };

    public string Casino1Logo => GetLogoPath(SelectedMatch?.Casino1);
    public string CasinoXLogo => GetLogoPath(SelectedMatch?.CasinoX);
    public string Casino2Logo => GetLogoPath(SelectedMatch?.Casino2);

    private string GetLogoPath(string casinoName)
    {
        if (string.IsNullOrWhiteSpace(casinoName))
            return "default_casino.png";

        return CasinoLogoMap.TryGetValue(casinoName.Trim(), out var logo)
            ? logo
            : "default_casino.png";
    }
    
    public ComparerDetail()
    {
        InitializeComponent();
        BindingContext = this;
    }
}
