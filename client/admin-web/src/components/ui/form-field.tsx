import { forwardRef } from "react"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { cn } from "@/lib/utils"

interface FormFieldProps extends React.ComponentProps<"input"> {
    label: string
    error?: string
}

export const FormField = forwardRef<HTMLInputElement, FormFieldProps>(
    ({ label, error, id, name, className, ...props }, ref) => {
        const fieldId = id ?? name
        return (
            <div className="space-y-1.5">
                <Label htmlFor={fieldId}>{label}</Label>
                <Input
                    id={fieldId}
                    name={name}
                    ref={ref}
                    aria-invalid={!!error}
                    className={cn(error && "border-red-500", className)}
                    {...props}
                />
                {error && <p className="text-sm text-red-500">{error}</p>}
            </div>
        )
    }
)
FormField.displayName = "FormField"