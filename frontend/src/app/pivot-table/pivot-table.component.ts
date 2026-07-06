import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { PivotCellItem, PivotResponse, PivotRow } from '../models/pivot.models';
import { SchedulePivotService } from '../services/schedule-pivot.service';

interface PivotFilters {
  project: string;
  product: string;
  version: string;
  testSuite: string;
}

@Component({
  selector: 'app-pivot-table',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pivot-table.component.html',
  styleUrl: './pivot-table.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PivotTableComponent implements OnInit {
  pivot: PivotResponse | null = null;
  loading = false;
  error: string | null = null;
  timezone = 'UTC';
  weekStart = '';

  filters: PivotFilters = {
    project: '',
    product: '',
    version: '',
    testSuite: ''
  };

  constructor(
    private readonly schedulePivotService: SchedulePivotService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadCurrentWeek();
  }

  get filteredRows(): PivotRow[] {
    if (!this.pivot) {
      return [];
    }

    return this.pivot.rows.filter(row => {
      return this.matchesFilter(row.project, this.filters.project)
        && this.matchesFilter(row.product, this.filters.product)
        && this.matchesFilter(row.version, this.filters.version)
        && this.rowContainsTestSuite(row, this.filters.testSuite);
    });
  }

  get displayedTestSuites(): string[] {
    if (!this.pivot) {
      return [];
    }

    return this.filters.testSuite
      ? [this.filters.testSuite]
      : this.pivot.testSuites;
  }

  get projectOptions(): string[] {
    return this.uniqueRowValues(row => row.project);
  }

  get productOptions(): string[] {
    return this.uniqueRowValues(row => row.product);
  }

  get versionOptions(): string[] {
    return this.uniqueRowValues(row => row.version);
  }

  loadCurrentWeek(): void {
    this.load(() => this.schedulePivotService.getCurrentWeekPivot(this.timezone));
  }

  loadSelectedWeek(): void {
    if (!this.weekStart) {
      this.loadCurrentWeek();
      return;
    }

    this.load(() => this.schedulePivotService.getPivot(this.weekStart, this.timezone));
  }

  moveWeek(offset: number): void {
    const currentWeekStart = this.weekStart || this.pivot?.weekStart;
    if (!currentWeekStart) {
      return;
    }

    this.weekStart = this.addDays(currentWeekStart, offset * 7);
    this.loadSelectedWeek();
  }

  resetFilters(): void {
    this.filters = {
      project: '',
      product: '',
      version: '',
      testSuite: ''
    };
  }

  cellItems(row: PivotRow, date: string, testSuite: string): PivotCellItem[] {
    return row.cells[this.cellKey(date, testSuite)] ?? [];
  }

  cellKey(date: string, testSuite: string): string {
    return `${date}|${testSuite}`;
  }

  trackDay(_: number, day: { date: string }): string {
    return day.date;
  }

  trackSuite(_: number, suite: string): string {
    return suite;
  }

  trackRow(_: number, row: PivotRow): string {
    return `${row.project}|${row.product}|${row.version}`;
  }

  trackCellItem(_: number, item: PivotCellItem): string {
    return `${item.scheduledTime}|${item.title}|${item.machine}|${item.test}`;
  }

  private load(requestFactory: () => ReturnType<SchedulePivotService['getCurrentWeekPivot']>): void {
    this.loading = true;
    this.error = null;

    requestFactory()
      .pipe(finalize(() => {
        this.loading = false;
        this.changeDetectorRef.markForCheck();
      }))
      .subscribe({
        next: response => {
          this.pivot = response;
          this.weekStart = response.weekStart;
        },
        error: (error: unknown) => {
          this.error = error instanceof Error ? error.message : 'Unable to load pivot data.';
        }
      });
  }

  private rowContainsTestSuite(row: PivotRow, testSuite: string): boolean {
    if (!testSuite) {
      return true;
    }

    return Object.keys(row.cells).some(cellKey => cellKey.endsWith(`|${testSuite}`));
  }

  private uniqueRowValues(selector: (row: PivotRow) => string): string[] {
    if (!this.pivot) {
      return [];
    }

    return Array.from(new Set(this.pivot.rows.map(selector))).sort((left, right) =>
      left.localeCompare(right)
    );
  }

  private matchesFilter(value: string, filter: string): boolean {
    return !filter || value === filter;
  }

  private addDays(dateValue: string, days: number): string {
    const [year, month, day] = dateValue.split('-').map(Number);
    const date = new Date(Date.UTC(year, month - 1, day));
    date.setUTCDate(date.getUTCDate() + days);

    return [
      date.getUTCFullYear(),
      String(date.getUTCMonth() + 1).padStart(2, '0'),
      String(date.getUTCDate()).padStart(2, '0')
    ].join('-');
  }
}
