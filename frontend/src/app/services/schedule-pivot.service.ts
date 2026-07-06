import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PivotResponse } from '../models/pivot.models';

@Injectable({ providedIn: 'root' })
export class SchedulePivotService {
  constructor(private readonly http: HttpClient) {}

  getCurrentWeekPivot(timezone = 'UTC'): Observable<PivotResponse> {
    const params = new HttpParams().set('timezone', timezone);
    return this.http.get<PivotResponse>('/api/pivot/current-week', {
      params,
      withCredentials: true
    });
  }

  getPivot(weekStart: string, timezone = 'UTC'): Observable<PivotResponse> {
    const params = new HttpParams()
      .set('weekStart', weekStart)
      .set('timezone', timezone);

    return this.http.get<PivotResponse>('/api/pivot', {
      params,
      withCredentials: true
    });
  }
}
