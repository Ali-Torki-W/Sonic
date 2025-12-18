import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { AppShell } from './core/layout/app-shell/app-shell';

@Component({
  selector: 'app-root',
  imports: [
    AppShell
  ],
  template: `<sonic-app-shell />`,
  // templateUrl: './app.html',
  // styleUrl: './app.scss'
  changeDetection: ChangeDetectionStrategy.OnPush,

})
export class App {
  protected readonly title = signal('sonic-web');
}
