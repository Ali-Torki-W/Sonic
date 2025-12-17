import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiClient {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = environment.apiBaseUrl.replace(/\/+$/, '');

    get<T>(path: string, params?: HttpParams) {
        return this.http.get<T>(`${this.baseUrl}${path}`, { params });
    }

    post<T>(path: string, body: unknown, params?: HttpParams) {
        return this.http.post<T>(`${this.baseUrl}${path}`, body, { params });
    }

    put<T>(path: string, body: unknown, params?: HttpParams) {
        return this.http.put<T>(`${this.baseUrl}${path}`, body, { params });
    }

    delete(path: string, params?: HttpParams) {
        return this.http.delete(`${this.baseUrl}${path}`, { params });
    }
}
