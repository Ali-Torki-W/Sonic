import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { ProblemDetails } from '../http/problem-details'; // Adjust path

export const problemDetailsInterceptor: HttpInterceptorFn = (req, next) => {
    return next(req).pipe(
        catchError((err: unknown) => {
            // We only handle HTTP errors. If it's a client-side JS error, let it pass.
            if (!(err instanceof HttpErrorResponse)) {
                return throwError(() => err);
            }

            const problem = normalizeProblemDetails(err);

            // Re-throw a new HttpErrorResponse where `error` is STRICTLY ProblemDetails
            const wrapped = new HttpErrorResponse({
                error: problem,
                headers: err.headers,
                status: err.status,
                statusText: err.statusText,
                url: err.url || undefined,
            });

            return throwError(() => wrapped);
        })
    );
};

function normalizeProblemDetails(err: HttpErrorResponse): ProblemDetails {
    const status = err.status; // 0 for network errors
    const fromBody = tryParseBody(err.error);

    // Merge defaults with backend response
    const result: ProblemDetails = {
        status: fromBody.status ?? status,
        title: fromBody.title ?? (status === 0 ? 'Connection Error' : err.statusText),
        type: fromBody.type,
        instance: fromBody.instance,
        // Default to a generic message if detail is missing
        detail: fromBody.detail || getDefaultMessage(status),
        errors: fromBody.errors,
        code: fromBody.code, // Keep backend code if exists
    };

    // UX Improvement: If it's a 400 Validation Error, promote the first field error to 'detail'
    // This allows UI to just show `err.error.detail` without digging into arrays.
    if (status === 400 && result.errors) {
        const firstKey = Object.keys(result.errors)[0];
        const firstMsg = result.errors[firstKey]?.[0];
        if (firstMsg) {
            result.detail = firstMsg;
        }
    }

    return result;
}

function tryParseBody(body: unknown): ProblemDetails {
    if (!body) return {};
    if (typeof body === 'object') return body as ProblemDetails;

    if (typeof body === 'string') {
        try {
            return JSON.parse(body) as ProblemDetails;
        } catch {
            return { detail: body };
        }
    }
    return {};
}

function getDefaultMessage(status: number): string {
    switch (status) {
        case 0: return 'Unable to reach the server. Check your connection.';
        case 400: return 'Invalid request data.';
        case 401: return 'Session expired. Please log in.';
        case 403: return 'You do not have permission to access this resource.';
        case 404: return 'Resource not found.';
        case 500: return 'Server error. Please try again later.';
        default: return 'An unexpected error occurred.';
    }
}