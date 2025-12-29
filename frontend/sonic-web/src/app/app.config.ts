import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth-interceptor';
import { errorStateInterceptor } from './core/interceptors/error-state.interceptor';
import { problemDetailsInterceptor } from './core/interceptors/problem-details.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),

    // CRITICAL: Order matters.
    // Response Flow: Backend -> ProblemDetails (Normalize) -> ErrorState (Side Effects) -> Auth
    provideHttpClient(
      withInterceptors([
        authInterceptor,
        errorStateInterceptor,
        problemDetailsInterceptor // Must be last to handle errors first
      ])
    ),
  ]
};