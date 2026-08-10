import { Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { PlayerDatabaseComponent } from './pages/player-database/player-database.component';

export const routes: Routes = [
  { path: '', component: DashboardComponent },
  { path: 'player-database', component: PlayerDatabaseComponent },
];
