import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

// Dates are ISO yyyy-MM-dd strings (from <input type="date">), which compare correctly as text.
export function dateRangeValidator(startKey: string, endKey: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const start: string = group.get(startKey)?.value;
    const end: string = group.get(endKey)?.value;
    if (!start || !end) return null;
    return start > end ? { dateRange: true } : null;
  };
}

export function inclusiveDays(start: string, end: string): number {
  const msPerDay = 24 * 60 * 60 * 1000;
  return Math.round((Date.parse(end) - Date.parse(start)) / msPerDay) + 1;
}
