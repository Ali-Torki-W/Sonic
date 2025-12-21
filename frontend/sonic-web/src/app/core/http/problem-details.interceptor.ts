import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import type { ProblemDetails } from './problem-details';

export const problemDetailsInterceptor: HttpInterceptorFn = (req, next) => {
    return next(req).pipe(
        catchError((err: unknown) => {
            if (!(err instanceof HttpErrorResponse)) {
                return throwError(() => err);
            }

            const normalized = normalizeProblemDetails(err);

            // Re-throw as HttpErrorResponse but with a consistent `error` body
            const wrapped = new HttpErrorResponse({
                error: normalized,
                headers: err.headers,
                status: err.status,
                statusText: err.statusText,
                url: err.url ?? undefined,
            });

            return throwError(() => wrapped);
        })
    );
};

function normalizeProblemDetails(err: HttpErrorResponse): ProblemDetails {
    const status = typeof err.status === 'number' ? err.status : 0;

    // Start with whatever backend sent
    const fromBody = readProblemFromBody(err.error);

    // If backend already sent a proper problem, keep it
    const base: ProblemDetails = {
        ...fromBody,
        status: fromBody.status ?? status,
        title: fromBody.title,
        detail: fromBody.detail,
        code: fromBody.code,
        errors: fromBody.errors,
    };

    // If we still don't have a useful message, synthesize one
    const hasDetail = typeof base.detail === 'string' && base.detail.trim().length > 0;
    if (!hasDetail) {
        base.detail = defaultMessageForStatus(status);
    }

    // If there's validation errors, prefer the first error as detail
    if (status === 400 && base.errors && Object.keys(base.errors).length > 0) {
        const firstKey = Object.keys(base.errors)[0];
        const firstMsg = base.errors[firstKey]?.[0];
        if (firstMsg && firstMsg.trim()) base.detail = firstMsg.trim();
    }

    // Provide a stable code if missing
    if (!base.code) {
        if (status === 401) base.code = 'unauthorized';
        else if (status === 403) base.code = 'forbidden';
        else if (status === 0) base.code = 'network-error';
        else base.code = String(status);
    }

    return base;
}

function readProblemFromBody(body: unknown): ProblemDetails {
    if (!body) return {};

    // If backend returns text, try parse JSON; otherwise treat as detail text
    if (typeof body === 'string') {
        const s = body.trim();
        if (!s) return {};
        try {
            const parsed = JSON.parse(s);
            if (parsed && typeof parsed === 'object') return parsed as ProblemDetails;
            return { detail: s };
        } catch {
            return { detail: s };
        }
    }

    if (typeof body === 'object') return body as ProblemDetails;
    return {};
}

function defaultMessageForStatus(status: number): string {
    switch (status) {
        case 0:
            return 'Network/CORS error: cannot reach API.';
        case 400:
            return 'Bad request.';
        case 401:
            return 'Unauthorized. Please log in and try again.';
        case 403:
            return 'Forbidden. You do not have permission to do this.';
        case 404:
            return 'Not found.';
        default:
            return 'Request failed.';
    }
}
