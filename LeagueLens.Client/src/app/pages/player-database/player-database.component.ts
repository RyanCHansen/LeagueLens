import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-player-database',
  standalone: true,
  templateUrl: './player-database.component.html',
  styleUrl: './player-database.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlayerDatabaseComponent {}
