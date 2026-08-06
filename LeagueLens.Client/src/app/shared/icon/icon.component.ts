import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type IconName =
  | 'dashboard'
  | 'team'
  | 'matchups'
  | 'trades'
  | 'draft'
  | 'database'
  | 'history'
  | 'analytics'
  | 'ai'
  | 'settings'
  | 'chevron-down'
  | 'chevron-right'
  | 'search'
  | 'sun'
  | 'bell'
  | 'menu';

@Component({
  selector: 'app-icon',
  standalone: true,
  templateUrl: './icon.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'inline-flex shrink-0' },
})
export class IconComponent {
  readonly name = input.required<IconName>();
}
