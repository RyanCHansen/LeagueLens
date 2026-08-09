import { Injectable, computed, signal } from '@angular/core';

import { LeagueSummary } from '../../Models/league-summary.model';

@Injectable({ providedIn: 'root' })
export class LeagueContextService {
  private readonly _leagues = signal<readonly LeagueSummary[]>([]);

  readonly leagues = this._leagues.asReadonly();
  readonly selectedLeague = computed<LeagueSummary | null>(() => this._leagues()[0] ?? null);
  readonly hasLeague = computed(() => this.selectedLeague() !== null);
}
