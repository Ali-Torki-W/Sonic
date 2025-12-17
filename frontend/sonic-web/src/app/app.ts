import { Component, signal } from '@angular/core';
import { AppShell } from './core/layout/app-shell/app-shell';

import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    AppShell
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'

})
export class App {
  protected readonly title = signal('sonic-web');
}
