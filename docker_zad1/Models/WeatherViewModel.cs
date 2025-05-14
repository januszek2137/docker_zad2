using docker_zad1.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace docker_zad1.Models {

    public class WeatherViewModel {

        [Display(Name = "Państwo")]
        public string SelectedCountry { get; set; } = "PL";

        [Display(Name = "Miasto")]
        public string SelectedCity { get; set; } = "Warsaw";

        public List<SelectListItem> CountryOptions { get; } = new() {
            new("Polska", "PL"),
            new("Niemcy", "DE"),
            new("Francja", "FR"),
            new("Włochy", "IT"),
            new("Hiszpania", "ES"),
            new("Szwajcaria", "CH"),
        };

        public List<SelectListItem> CityOptions { get; private set; } = new();

        public WeatherDto? Weather { get; set; }

        public WeatherViewModel() => FillCities();

        public void FillCities() {
            CityOptions = SelectedCountry switch {
                "PL" => new() { new("Warszawa", "Warsaw"), new("Kraków", "Krakow"), new("Wrocław", "Wroclaw") },
                "DE" => new() { new("Berlin", "Berlin"), new("Monachium", "Munich"), new("Hamburg", "Hamburg") },
                "FR" => new() { new("Paryż", "Paris"), new("Marsylia", "Marseille"), new("Lyon", "Lyon") },
                "IT" => new() { new("Rzym", "Rome"), new("Mediolan", "Milan"), new("Neapol", "Naples") },
                "ES" => new() { new("Madryt", "Madrid"), new("Barcelona", "Barcelona"), new("Walencja", "Valencia") },
                "CH" => new() { new("Zurych", "Zurich"), new("Genewa", "Geneva"), new("Bazylea", "Basel") },
                _ => new() { new("Warszawa", "Warsaw"), new("Kraków", "Krakow"), new("Wrocław", "Wroclaw") }
            };
        }
    }
}