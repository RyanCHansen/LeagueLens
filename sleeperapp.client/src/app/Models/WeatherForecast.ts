export interface WeatherForecastDTO {
  date: string; // ISO 8601 date string
  temperatureC: number;
  summary?: string | null;
}

export class WeatherForecast {
  public date: Date;
  public temperatureC: number;
  public summary?: string | null;

  constructor(date: Date, temperatureC: number, summary?: string | null) {
    this.date = date;
    this.temperatureC = temperatureC;
    this.summary = summary ?? null;
  }

  // Create instance from a DTO or JSON string
  public static from(dto: WeatherForecastDTO | string): WeatherForecast {
    const parsed: WeatherForecastDTO =
      typeof dto === "string" ? JSON.parse(dto) : dto;

    const date = parsed.date ? new Date(parsed.date) : new Date();
    const temperatureC = Number(parsed.temperatureC) || 0;
    const summary = parsed.summary ?? null;

    return new WeatherForecast(date, temperatureC, summary);
  }

  // Convert to a serializable DTO
  public toDTO(): WeatherForecastDTO {
    return {
      date: this.date.toISOString(),
      temperatureC: this.temperatureC,
      summary: this.summary ?? null,
    };
  }

  // JSON serialization
  public toJSON(): string {
    return JSON.stringify(this.toDTO());
  }

  // Computed Fahrenheit value
  public get temperatureF(): number {
    return Math.round((this.temperatureC * 9) / 5 + 32);
  }

  // Basic validation
  public isValid(): boolean {
    return (
      this.date instanceof Date &&
      !Number.isNaN(this.date.getTime()) &&
      typeof this.temperatureC === "number" &&
      Number.isFinite(this.temperatureC)
    );
  }

  // Useful string representation for debugging
  public toString(): string {
    return `WeatherForecast { date: ${this.date.toISOString()}, temperatureC: ${this.temperatureC}, temperatureF: ${this.temperatureF}, summary: ${this.summary} }`;
  }
}
