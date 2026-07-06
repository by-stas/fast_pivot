import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PivotTableComponent } from './pivot-table/pivot-table.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [PivotTableComponent],
  template: `
    <main class="app-shell">
      <app-pivot-table />
    </main>
  `,
  styles: [
    `
      .app-shell {
        min-height: 100vh;
        padding: 24px;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {}
