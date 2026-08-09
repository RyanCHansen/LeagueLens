import { ChangeDetectionStrategy, Component } from '@angular/core';

import { IconComponent } from '../../../shared/icon/icon.component';

@Component({
  selector: 'app-league-onboarding',
  standalone: true,
  imports: [IconComponent],
  templateUrl: './league-onboarding.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LeagueOnboardingComponent {}
