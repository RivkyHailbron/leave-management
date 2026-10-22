import { FormControl, FormGroup } from '@angular/forms';
import { dateRangeValidator, inclusiveDays } from './date-range.validator';

describe('dateRangeValidator', () => {
  const validate = (startDate: string, endDate: string) =>
    dateRangeValidator('startDate', 'endDate')(
      new FormGroup({ startDate: new FormControl(startDate), endDate: new FormControl(endDate) })
    );

  it('accepts an end date after the start date', () => {
    expect(validate('2026-08-02', '2026-08-06')).toBeNull();
  });

  it('accepts a single-day range', () => {
    expect(validate('2026-08-02', '2026-08-02')).toBeNull();
  });

  it('rejects an end date before the start date', () => {
    expect(validate('2026-08-06', '2026-08-02')).toEqual({ dateRange: true });
  });

  it('leaves missing dates to the required validator', () => {
    expect(validate('', '2026-08-02')).toBeNull();
  });
});

describe('inclusiveDays', () => {
  it('counts both the start and the end day', () => {
    expect(inclusiveDays('2026-08-02', '2026-08-06')).toBe(5);
    expect(inclusiveDays('2026-08-02', '2026-08-02')).toBe(1);
  });
});
