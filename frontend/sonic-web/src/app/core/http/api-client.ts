import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiClient {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = environment.apiBaseUrl.replace(/\/+$/, '');

    // Note: We don't need catchError() here anymore.
    // The interceptors ensure that if an error arrives here, it is already clean.

    get<T>(path: string, params?: HttpParams): Observable<T> {
        return this.http.get<T>(this.url(path), { params });
    }

    post<T>(path: string, body: unknown, params?: HttpParams): Observable<T> {
        return this.http.post<T>(this.url(path), body, { params });
    }

    put<T>(path: string, body: unknown, params?: HttpParams): Observable<T> {
        return this.http.put<T>(this.url(path), body, { params });
    }

    delete<T>(path: string, params?: HttpParams): Observable<T> {
        return this.http.delete<T>(this.url(path), { params });
    }

    private url(path: string): string {
        return `${this.baseUrl}${path}`;
    }
}