import { ChangeDetectionStrategy, Component, output, signal } from '@angular/core';

import { IconComponent } from '../../shared/icon/icon.component';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [IconComponent],
  templateUrl: './topbar.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopbarComponent {
  readonly menuToggle = output<void>();

  protected readonly searchTerm = signal('');

  protected onSearchInput(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }
}
