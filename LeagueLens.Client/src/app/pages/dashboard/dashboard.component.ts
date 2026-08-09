import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { LeagueContextService } from '../../core/league-context/league-context.service';
import { LeagueOnboardingComponent } from './league-onboarding/league-onboarding.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [LeagueOnboardingComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent {
  protected readonly leagueContext = inject(LeagueContextService);
}
