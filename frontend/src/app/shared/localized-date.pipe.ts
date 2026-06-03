import { DatePipe } from '@angular/common';
import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Pipe({
  name: 'localizedDate',
  standalone: true,
  pure: false,
})
export class LocalizedDatePipe implements PipeTransform {
  private readonly datePipe = new DatePipe('en-US');
  private readonly translate = inject(TranslateService);

  transform(value: string | number | Date | null | undefined, format?: string, timezone?: string): string | null {
    const locale = this.translate.currentLang === 'uk' ? 'uk-UA' : 'en-US';
    return this.datePipe.transform(value, format, timezone, locale);
  }
}
