import { 
  Component, Input, Output, EventEmitter, signal, computed, 
  ElementRef, HostListener, forwardRef, ViewChild, ViewChildren, QueryList 
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { COUNTRY_CODES, CountryCode } from '../../../core/constants/country-codes.constant';

@Component({
  selector: 'app-country-picker',
  host: { '[class.open]': 'isOpen()' },
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './country-picker.html',
  styleUrl: './country-picker.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CountryPickerComponent),
      multi: true
    }
  ]
})
export class CountryPickerComponent implements ControlValueAccessor {
  @Input() countries: CountryCode[] = COUNTRY_CODES;
  @Output() countrySelected = new EventEmitter<CountryCode>();

  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;
  @ViewChild('optionsList') optionsList!: ElementRef<HTMLDivElement>;

  isOpen = signal(false);
  searchQuery = signal('');
  selectedCode = signal('+20');
  activeIndex = signal<number>(0);

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  selectedCountry = computed(() => {
    const code = this.selectedCode();
    return this.countries.find(c => c.code === code) || this.countries[0];
  });

  filteredCountries = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    if (!q) return this.countries;
    return this.countries.filter(c => 
      c.name.toLowerCase().includes(q) || 
      c.code.toLowerCase().includes(q)
    );
  });

  toggleDropdown(): void {
    if (this.isOpen()) {
      this.close();
    } else {
      this.open();
    }
  }

  open(): void {
    this.isOpen.set(true);
    this.searchQuery.set('');

    // Find initial index of currently selected country
    const list = this.filteredCountries();
    const idx = list.findIndex(c => c.code === this.selectedCode());
    this.activeIndex.set(idx >= 0 ? idx : 0);

    setTimeout(() => {
      if (this.searchInput) {
        this.searchInput.nativeElement.focus();
      }
      this.scrollToActive();
    }, 100);
  }

  close(): void {
    this.isOpen.set(false);
    this.onTouched();
  }

  selectCountry(country: CountryCode): void {
    this.selectedCode.set(country.code);
    this.onChange(country.code);
    this.countrySelected.emit(country);
    this.close();
  }

  onSearchInput(query: string): void {
    this.searchQuery.set(query);
    this.activeIndex.set(0); // Reset highlight to first result when searching
  }

  @HostListener('keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    if (!this.isOpen()) {
      if (['ArrowDown', 'ArrowUp', 'Enter', ' '].includes(event.key)) {
        event.preventDefault();
        this.open();
      }
      return;
    }

    const list = this.filteredCountries();
    if (!list.length) {
      if (event.key === 'Escape') {
        event.preventDefault();
        this.close();
      }
      return;
    }

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        const next = (this.activeIndex() + 1) % list.length;
        this.activeIndex.set(next);
        this.scrollToActive();
        break;

      case 'ArrowUp':
        event.preventDefault();
        const prev = (this.activeIndex() - 1 + list.length) % list.length;
        this.activeIndex.set(prev);
        this.scrollToActive();
        break;

      case 'Enter':
        event.preventDefault();
        if (this.activeIndex() >= 0 && this.activeIndex() < list.length) {
          this.selectCountry(list[this.activeIndex()]);
        }
        break;

      case 'Escape':
        event.preventDefault();
        this.close();
        break;

      case 'Tab':
        this.close();
        break;
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      if (this.isOpen()) {
        this.close();
      }
    }
  }

  private scrollToActive(): void {
    setTimeout(() => {
      if (!this.optionsList) return;
      const container = this.optionsList.nativeElement;
      const activeEl = container.querySelector('.option-item.active-highlight') as HTMLElement;
      if (activeEl) {
        activeEl.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
      }
    }, 0);
  }

  constructor(private elementRef: ElementRef) {}

  // ControlValueAccessor implementation
  writeValue(value: string): void {
    if (value) {
      this.selectedCode.set(value);
    }
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }
}
