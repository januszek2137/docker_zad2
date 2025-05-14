using docker_zad1.Models;
using docker_zad1.Services;
using Microsoft.AspNetCore.Mvc;

namespace docker_zad1.Controllers {

    public class WeatherController : Controller {
        public readonly IWeatherService _weatherService;

        public WeatherController(IWeatherService weatherService) => _weatherService = weatherService;

        public IActionResult Index() => View(new WeatherViewModel());  // GET /Weather

        [HttpPost]
        public async Task<IActionResult> Index(WeatherViewModel vm) {
            if(!ModelState.IsValid)
                return View(vm);

            vm.Weather = await _weatherService.GetCurrentAsync(vm.SelectedCity);
            vm.FillCities();
            return View(vm);
        }
    }
}