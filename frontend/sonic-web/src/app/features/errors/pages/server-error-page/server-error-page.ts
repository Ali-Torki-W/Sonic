import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from "@angular/router";

@Component({
  selector: 'app-server-error-page',
  imports: [RouterLink],
  templateUrl: './server-error-page.html',
  styleUrl: './server-error-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServerErrorPage {
  retry(): void {
    window.location.reload();
  }
}
