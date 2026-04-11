import { HttpClient } from '@angular/common/http';
import { Component, signal, OnInit } from '@angular/core';
import { WeatherForecast } from './Models/WeatherForecast';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.css'
})
export class App implements OnInit {

  public forecasts: WeatherForecast[] = [];
  isLoading = false;
  error: string | null = null;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.getForecasts();
  }

  getForecasts() {
    this.http.get<WeatherForecast[]>('/weatherforecast').subscribe(
      (data) => {
        this.forecasts = data;
      },
      (error) => {
        console.error(error);
      }
    );
  }

  getForecastsButton() {
    this.isLoading = true;
    this.error = null;

    this.http.get<WeatherForecast[]>('/weatherforecast').subscribe({
      next: (data) => {
        this.forecasts = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.error = err.message || 'An error occurred';
        this.isLoading = false;
      }
    });
  }

  protected readonly title = signal('sleeperapp.client');
}
