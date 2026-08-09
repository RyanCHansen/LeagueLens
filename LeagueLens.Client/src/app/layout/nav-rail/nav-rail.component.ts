import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { LeagueContextService } from '../../core/league-context/league-context.service';
import { IconComponent, IconName } from '../../shared/icon/icon.component';

interface NavItem {
  readonly label: string;
  readonly icon: IconName;
  readonly route: string | null;
}

@Component({
  selector: 'app-nav-rail',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, IconComponent],
  templateUrl: './nav-rail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NavRailComponent {
  protected readonly leagueContext = inject(LeagueContextService);

  readonly open = input(false);
  readonly navigated = output<void>();

  protected readonly navItems: readonly NavItem[] = [
    { label: 'Dashboard', icon: 'dashboard', route: '/' },
    { label: 'My Team', icon: 'team', route: null },
    { label: 'Matchups', icon: 'matchups', route: null },
    { label: 'Trades', icon: 'trades', route: null },
    { label: 'Draft Picks', icon: 'draft', route: null },
    { label: 'Player Database', icon: 'database', route: null },
    { label: 'League History', icon: 'history', route: null },
    { label: 'Analytics', icon: 'analytics', route: null },
    { label: 'AI Assistant', icon: 'ai', route: null },
  ];

  protected readonly settingsItem: NavItem = { label: 'Settings', icon: 'settings', route: null };
}
