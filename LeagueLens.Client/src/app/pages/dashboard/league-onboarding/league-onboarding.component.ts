import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { IconComponent } from '../../../shared/icon/icon.component';

@Component({
  selector: 'app-league-onboarding',
  standalone: true,
  imports: [RouterLink, IconComponent],
  templateUrl: './league-onboarding.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LeagueOnboardingComponent {
  protected readonly showConnectForm = signal(false);
  protected readonly submitMessage = signal<string | null>(null);

  protected toggleConnectForm(): void {
    this.showConnectForm.update((show) => !show);
  }

  protected submitLeagueId(value: string): void {
    if (!value.trim()) {
      return;
    }
    this.submitMessage.set("League sync isn't available yet — check back soon.");
  }
}
