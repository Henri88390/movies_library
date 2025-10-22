import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  Output,
  forwardRef,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-star-rating',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './star-rating.component.html',
  styleUrl: './star-rating.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => StarRatingComponent),
      multi: true,
    },
  ],
})
export class StarRatingComponent implements ControlValueAccessor {
  @Input() disabled = false;
  @Input() readonly = false;
  @Output() ratingChange = new EventEmitter<number>();

  private _rating = 0;
  private onChange = (value: number) => {};
  private onTouched = () => {};

  get rating(): number {
    return this._rating;
  }

  set rating(value: number) {
    this._rating = value;
    this.onChange(value);
    this.ratingChange.emit(value);
  }

  stars = [1, 2, 3, 4, 5];
  hoveredRating = 0;

  // ControlValueAccessor implementation
  writeValue(value: number): void {
    this._rating = value || 0;
  }

  registerOnChange(fn: (value: number) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  // Star interaction methods
  onStarClick(starIndex: number, isHalf: boolean = false): void {
    if (this.disabled || this.readonly) return;

    const newRating = starIndex * 2 + (isHalf ? -1 : 0); // Convert to 0-10 scale
    this.rating = newRating;
    this.onTouched();
  }

  onStarHover(starIndex: number, isHalf: boolean = false): void {
    if (this.disabled || this.readonly) return;

    this.hoveredRating = starIndex * 2 + (isHalf ? -1 : 0);
  }

  onMouseLeave(): void {
    this.hoveredRating = 0;
  }

  getStarClass(starIndex: number): string {
    const currentRating = this.hoveredRating || this.rating;
    const starValue = starIndex * 2; // Full star value (2, 4, 6, 8, 10)

    if (currentRating >= starValue) {
      return 'full';
    } else if (currentRating === starValue - 1) {
      return 'half';
    } else {
      return 'empty';
    }
  }

  getRatingText(): string {
    return `${this.rating}/10`;
  }
}
